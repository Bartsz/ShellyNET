## Tested with real device, but still consider this an Alpha version under development.

# ShellyNET

**A performance-focused .NET client for Shelly Gen2+ devices over WebSocket JSON-RPC.**

ShellyNET is built around one idea: **processing frequent device-state updates should be fast and should create as little garbage as possible**. Instead of deserializing every notification into an object graph, it reads UTF-8 JSON directly, extracts the relevant properties, and publishes compact, typed messages.

The project prioritizes predictable memory usage, low allocation pressure, and efficient processing of continuous Shelly notifications. It also provides typed RPC commands, device authentication, and support for BTHome sensor updates.

## Features

- **Persistent WebSocket RPC connection** to Shelly Gen2+ devices, with `ws://` and `wss://` transport support.
- **Low-allocation status processing** for `NotifyStatus` and `NotifyFullStatus` notifications.
- **Hand-written, order-aware JSON parsers** built on `Utf8JsonReader`, without materializing full JSON object graphs for normal status updates.
- **Compact value-type notifications** for numeric, boolean, and nullable component state changes.
- **Component event notifications** via `NotifyEvent`.
- **Typed RPC commands** for switches, covers, lights, CCT/RGB/RGBCCT/RGBW, scripts, system operations, and more.
- **Shelly digest authentication**, including authentication challenges and handling of stale reusable nonces.
- **BTHome sensor notifications**, mapped to device addresses and BTHome object types after configuration is loaded.
- **Pooled buffers and bounded channels** for message handling and backpressure.
- **Explicit RPC result states**, cancellation support, and unexpected-disconnection notifications.

## Performance-first design

The most important optimization is in the **incoming status-notification path**, where a device may report individual component properties continuously.

### Parsing UTF-8 directly

Rather than using `JsonSerializer` and a source-generated serialization context to deserialize every incoming status notification, ShellyNET uses dedicated parsers that operate directly on the message's UTF-8 bytes.

- `Utf8JsonReader` advances through the payload without creating a JSON document.
- Property names are compared against UTF-8 literals using spans, avoiding temporary strings for routine lookups.
- The parser is optimized for the property order commonly emitted by Shelly devices. **The usual path is a single forward pass**, but supported properties are not required to arrive in that exact order; fallback logic (including timestamp lookahead when necessary) accommodates out-of-order input.
- Parsed primitive changes are represented with value types instead of allocating an object per property.

This is deliberately specialized code: more involved than general-purpose deserialization, but designed to minimize work in a frequently executed path.

### Memory reuse and backpressure

WebSocket frames are read into buffers rented from `ArrayPool<byte>`. The receive buffer grows when needed and is returned to the pool. Notification parsers also rent arrays for their results, and the receive loop returns those arrays after forwarding the messages.

The client uses bounded `System.Threading.Channels` to separate transport and message consumers. When a channel fills up, the producer waits rather than growing an unbounded queue.

A typical status update becomes a `ShellyNotifyStatusMessage` (`readonly struct`) carrying the component, property, timestamp, and value. For boolean or nullable state changes, consumers can use helpers such as `AsBool` and `IsExplicitNull`.

### Deliberate trade-offs

**Low allocation does not mean that every operation in the library is allocation-free.** Some payloads contain strings or complex values; full configuration/status requests, RPC serialization, and authentication have different requirements than the notification hot path.

Events are also more heterogeneous than simple state changes. The currently connected `NotifyEvent` pipeline exposes compact `ShellyComponentEvent` value types. A separate richer typed-event parser/model exists in the source (`Parsers123`) and uses class instances for event-specific payloads, but it is **not yet the parser connected to the public event-reading API**.

**Important for performance measurements:** the current send and receive loops contain diagnostic `Console.WriteLine` calls that decode every frame using `Encoding.UTF8.GetString(...)`. Those calls allocate a string per frame and add console-I/O overhead. Remove or disable them before benchmarking the low-allocation notification path. No throughput or allocation benchmark numbers are claimed here.

## Requirements and integration

- **.NET 9 or later** (the source uses APIs such as `ClientWebSocketOptions.KeepAliveTimeout`).
- A Shelly device supporting the **Gen2+ JSON-RPC WebSocket protocol**, reachable by IP address.
- A reference to the `ShellyNET.Client` project in your .NET solution, including its `ShellyJsonSerializerContext` source-generation metadata used for typed RPC serialization.

Reference the project from your application as a source project. This README does not assume a published NuGet package.

## Quick start

The example below connects to one Shelly device, authenticates if necessary, executes a switch command, and processes live state changes until **Ctrl+C**.

```csharp
using System.Net;
using ShellyNET.Client;
using ShellyNET.Client.Model;
using ShellyNET.Client.Model.Enums;

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    stop.Cancel();
};

var client = new ShellyClient();
var connection = await client.ConnectAsync(IPAddress.Parse("192.168.1.50"));

if (connection != ConnectionResult.Connected)
{
    Console.WriteLine($"Connection failed: {connection}");
    return;
}

try
{
    if (client.DeviceInfo?.AuthenticationEnabled == true)
    {
        // Replace with the actual password, preferably loaded from a secret store.
        var login = await client.LoginToDeviceAsync("YOUR_PASSWORD", stop.Token);
        if (!login.IsSuccess)
        {
            Console.WriteLine($"Authentication failed: {login.Status}");
            return;
        }
    }

    var command = await client.CallSwitchSetAsync(
        componentId: 0,
        state: true,
        cancellationToken: stop.Token);

    Console.WriteLine($"Switch.Set: {command.Status}");

    while (!stop.IsCancellationRequested)
    {
        var change = await client.GetShellyStatusMessageAsync(stop.Token);

        if (change.ComponentType == ComponentType.Switch &&
            change.PropertyName == PropertyName.Output)
        {
            Console.WriteLine($"Switch {change.Id}: {change.AsBool}");
        }
    }
}
catch (OperationCanceledException) when (stop.IsCancellationRequested)
{
    // Normal shutdown requested by the user.
}
finally
{
    await client.DisconnectAsync();
}
```

A `NotifyStatus` message is a **delta**, not necessarily a complete device state. Build and maintain a complete state in your application by applying incoming changes, or obtain an initial snapshot with `Call_Shelly_GetDeviceStatus(...)` when appropriate.

## Reading component events

`GetShellyEventAsync` reads compact event notifications, including supported input events:

```csharp
// Start this from a separate task after connecting.
static async Task ReadEventsAsync(ShellyClient client, CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        var evt = await client.GetShellyEventAsync(cancellationToken);

        if (evt.ComponentType == ComponentType.Input &&
            evt.ComponentEvent == ComponentEvent.SinglePush)
        {
            Console.WriteLine($"Input {evt.Id}: single press at {evt.Timestamp:O}");
        }
    }
}
```

`GetShellyStatusMessageAsync`, `GetShellyEventAsync`, and the BTHome reader use separate bounded channels. The channels are configured for **one reader each**; consume them from independent tasks if you need multiple notification streams simultaneously.

## BTHome sensors

BTHome sensor notifications can be consumed separately from ordinary component-state changes. **Load the device configuration first**: the client uses `Call_Shelly_GetDeviceConfigAsync` to build its internal mapping from BTHome component IDs to sensor MAC addresses, indices, and object types.

```csharp
static async Task ReadBTHomeAsync(ShellyClient client, CancellationToken cancellationToken)
{
    var config = await client.Call_Shelly_GetDeviceConfigAsync(cancellationToken);
    if (!config.IsSuccess)
    {
        Console.WriteLine($"Cannot load component configuration: {config.Status}");
        return;
    }

    while (!cancellationToken.IsCancellationRequested)
    {
        var sensor = await client.GetBTHomeSensorMessage(cancellationToken);

        Console.WriteLine(
            $"Sensor {sensor.MacAddress:X12}, index {sensor.Idx}, " +
            $"object {sensor.ObjectType}: " +
            (sensor.Text ?? sensor.Value.ToString()));
    }
}
```

## RPC API overview

Public methods on `ShellyClient` include:

| Area | Examples |
| --- | --- |
| Connection | `ConnectAsync`, `DisconnectAsync`, `LoginToDeviceAsync` |
| Device information | `Call_Shelly_GetDeviceInfoAsync`, `Call_Shelly_GetDeviceConfigAsync`, `Call_Shelly_GetDeviceStatus` |
| Switches | `CallSwitchSetAsync`, `CallSwitchToggleAsync` |
| Covers | `CallCoverOpenAsync`, `CallCoverCloseAsync`, `CallCoverStopAsync`, `CallCoverGoToPositionAsync`, `CallCoverCalibrateAsync` |
| Lighting | `CallLightSetAsync`, `CallLightToggleAsync`, `CallCctSetAsync`, `CallRgbSetAsync`, `CallRgbCctSetAsync`, `CallRgbWSetAsync` |
| Device and system | `CallShellyRebootAsync`, `CallSysSetTimeAsync`, `Call_Shelly_CheckForUpdate`, `Call_Shelly_Reboot` |
| Scripting and BLE | `CallScriptEvalAsync`, `CallBTHomeAddDeviceAsync` |
| Notifications | `GetShellyStatusMessageAsync`, `GetShellyEventAsync`, `GetBTHomeSensorMessage` |

Methods returning `ShellyRpcResult<T>` expose `Status`, `Value`, `RpcError`, `ErrorMessage`, and `IsSuccess`. Expected failures (such as a timeout or a device-side RPC error) are represented as result states. Some command helpers instead return a plain `ValueTask`; they wait for the response but do not expose an RPC result to the caller.

Individual commands validate their parameters, such as component IDs, percentages, mutually exclusive arguments, durations, and RPC tags.

## Connection behavior

A `ShellyClient` instance represents a persistent connection to **one device**. It first attempts `ws://<device-ip>:80/rpc`, then falls back to `wss://<device-ip>:443/rpc` if necessary. Successful initialization retrieves device information automatically.

The client has separate send/receive loops, correlates RPC responses with request IDs, supports cancellation, and applies a **10-second default RPC response timeout**. It exposes `DisconnectedUnexpectedly`, `UnsupportedMessageTypeReceived`, and `UnknownMessageReceived` events for diagnostics. Automatic reconnection is not currently implemented; applications should handle a lost connection explicitly.

**Security note:** in the current implementation, the `wss://` fallback accepts untrusted server certificates. That avoids connection failures on devices using self-signed certificates, but it does **not** authenticate the remote endpoint. Deploy only on trusted networks or strengthen certificate validation before using the client across untrusted networks. The plain `ws://` transport is not encrypted.

## Project structure

```text
ShellyNET.Client/
├── Client/
│   ├── ShellyClient.Connection.cs     # Connections, authentication, RPC correlation
│   ├── ShellyClient.ReceivingLoop.cs  # WebSocket reads and notification routing
│   ├── ShellyClient.SendingLoop.cs    # Outbound queue and WebSocket writes
│   ├── ShellyClient.RPC.cs            # Typed component RPC commands
│   └── ShellyClient.RPC.Shelly.cs     # Device-level and BTHome RPC methods
├── Model/                         # Status values, configs, enums, RPC models
├── Parsers/                       # Hand-written status and event parsers
└── ShellyComponentsConfigParser.cs
```

## Protocol references

- [Shelly Gen2+ RPC protocol](https://shelly-api-docs.shelly.cloud/gen2/General/RPCProtocol/)
- [Shelly RPC over WebSocket](https://shelly-api-docs.shelly.cloud/gen2/General/RPCChannels/)
- [Shelly notifications (`NotifyStatus`, `NotifyFullStatus`, `NotifyEvent`)](https://shelly-api-docs.shelly.cloud/gen2/General/Notifications/)

## License

ShellyNET is licensed under the **[GNU Affero General Public License v3.0 (AGPL-3.0)](https://www.gnu.org/licenses/agpl-3.0.en.html)**. See the repository's `LICENSE` file for the complete license text.
