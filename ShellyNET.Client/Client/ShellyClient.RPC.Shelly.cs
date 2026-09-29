using ShellyNET.Client.Model;
using ShellyNET.Client.Model.Enums;
using ShellyNET.Client.Model.Messages;
using ShellyNET.Client.Parsers;

namespace ShellyNET.Client;

public partial class ShellyClient
{

    /// <summary>Loads device identity without authentication and refreshes the information used by connection initialization.</summary>
    public async Task<ShellyRpcResult<ShellyDeviceInfo>> Call_Shelly_GetDeviceInfoAsync(CancellationToken ct = default)
    {
        var result = await SendRpcAttemptAsync<EmptyRpcParameters, ShellyDeviceInfo>(RpcMethods.Shelly_GetDeviceInfo, null, null, static (_, resultPayload) => JsonSerializer.Deserialize(resultPayload.Span, ShellyJsonSerializerContext.Default.ShellyDeviceInfo), ct).ConfigureAwait(false);
        if (result.Status != ShellyRpcResultStatus.Success) return result;
        if (result.Value is null) return new ShellyRpcResult<ShellyDeviceInfo>(ShellyRpcResultStatus.ClientError, null, null, "Shelly.GetDeviceInfo returned an empty result.");

        Volatile.Write(ref _deviceInfo, result.Value);
        if (!result.Value.AuthenticationEnabled) Interlocked.Exchange(ref _authenticationState, null);
        return result;
    }

    /// <summary>Loads the complete device configuration and converts component entries into their strongly typed configuration models.</summary>
    public async Task<ShellyRpcResult<List<ShellyComponentConfigBase>>> Call_Shelly_GetDeviceConfigAsync(CancellationToken ct = default)
    {
        var response = await SendRpcAsync<EmptyRpcParameters, List<ShellyComponentConfigBase>>(RpcMethods.Shelly_GetConfig, null, (message, response) =>
        {
            var config = _configParser.ParseConfig(response);
            var bthomesensors = config.OfType<BTHomeSensorConfig>();
            var maxComponentIndex = bthomesensors is null ? 0 : bthomesensors.Any() ? bthomesensors.Max(x => x.ComponentId) + 1 : 0;


            // exhchange is dont here since this is executed in context of message recieving loop so nothing can use any parser or array 
            // and it is safe to exchange almost enything in the client.
            _btHomeSensorMacAddressById = new long[maxComponentIndex];
            _btHomeSensorIdxById = new byte[maxComponentIndex];
            _btHomeObjectTypeById = new BTHomeObjectType[maxComponentIndex];

            if (bthomesensors is not null)
            {
                foreach (var item in bthomesensors)
                {
                    _btHomeSensorMacAddressById[item.ComponentId] = item.MacAddress;
                    _btHomeSensorIdxById[item.ComponentId] = item.Idx;
                    _btHomeObjectTypeById[item.ComponentId] = item.ObjectType;
                }
            }

            return config;
        }, ct).ConfigureAwait(false);
        if (!response.IsSuccess) return new ShellyRpcResult<List<ShellyComponentConfigBase>>(response.Status, null, response.RpcError, response.ErrorMessage);

        try
        {
            return new ShellyRpcResult<List<ShellyComponentConfigBase>>(ShellyRpcResultStatus.Success, response.Value, null, null);
        }
        catch (JsonException exception)
        {
            return new ShellyRpcResult<List<ShellyComponentConfigBase>>(ShellyRpcResultStatus.ClientError, null, null, exception.Message);
        }
    }

    /// <summary>Loads the complete device status and publishes it through the existing component-state processing path.</summary>
    public async Task<ShellyRpcResult<IShellyStatusMessage[]>> Call_Shelly_GetDeviceStatus(CancellationToken ct = default)
    {
        var response = await SendRpcAsync<EmptyRpcParameters, ShellyNotifyStatusParseResult>(RpcMethods.Shelly_GetStatus, null, (message, _) =>
        {
            var parser = new ShellyNotifyStatusParser();
            return parser.ProcessNotifyStatusMessage(message, StatesContainingObject.Results, ct);
        }, ct).ConfigureAwait(false);

        if (!response.IsSuccess) return new ShellyRpcResult<IShellyStatusMessage[]>(response.Status, null, response.RpcError, response.ErrorMessage);

        var messages = response.Value.MessageSpan.ToArray().OfType<IShellyStatusMessage>();
        var textMessages = response.Value.StringMessageSpan.ToArray().OfType<IShellyStatusMessage>();
        var allMessages = messages.Concat(textMessages).ToArray();
        response.Value.Dispose();
        return new ShellyRpcResult<IShellyStatusMessage[]>(ShellyRpcResultStatus.Success, allMessages, null, null);
    }

    /// <summary>Checks whether a firmware update is available for the connected Shelly device.</summary>
    public Task<ShellyRpcResult<DeviceUpdateResult>> Call_Shelly_CheckForUpdate(CancellationToken ct = default)
    {
        return SendRpcAsync<DeviceUpdateResult>(RpcMethods.Shelly_CheckForUpdate, ct);
    }

    /// <summary>Requests an immediate device reboot and returns the RPC result reported by Shelly.</summary>
    public Task<ShellyRpcResult<NullRpcResponse>> Call_Shelly_Reboot(CancellationToken ct = default)
    {
        return SendRpcAsync<NullRpcResponse>(RpcMethods.Shelly_Reboot, ct);
    }

    /// <summary>
    /// Executes JavaScript code inside an already running Shelly script.
    /// </summary>
    public async ValueTask<ShellyRpcResult<NullRpcResponse>> CallScriptEvalAsync(int scriptId, string code, CancellationToken ct = default)
    {
        if (scriptId < 0) throw new ArgumentOutOfRangeException(nameof(scriptId));

        if (string.IsNullOrWhiteSpace(code)) return new(ShellyRpcResultStatus.ClientError, default, null, "No JS code was provided");

        var startParams = new StartScriptParams(scriptId);
        var parameters = new ScriptEvalParams(scriptId, code);

        var responseRunning = await SendRpcAsync<StartScriptParams, NullRpcResponse>(RpcMethods.Script_Start, startParams, ct);
        var response = await SendRpcAsync<ScriptEvalParams, NullRpcResponse>(RpcMethods.Script_Eval, parameters, ct);

        return response;
    }

    public ValueTask<ShellyRpcResult<BTHomeAddDeviceResult>> CallBTHomeAddDeviceAsync(string address, string? name = null, string? encryptionKey = null, int? componentId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("BTHome device address cannot be empty.",nameof(address));

        if (componentId is < 200 or > 299) throw new ArgumentOutOfRangeException(nameof(componentId),"BTHome component id must be in range 200..299.");

        var config = new BTHomeAddDeviceConfig(address, name, encryptionKey);
        var parameters = new BTHomeAddDeviceParams(config, componentId);

        return SendRpcAsync<BTHomeAddDeviceParams, BTHomeAddDeviceResult>(RpcMethods.BTHome_AddDevice, parameters, ct);
    }

    //public ValueTask<ShellyRpcResult<BleScanStartResult>> CallBleScanStartAsync(int scriptId, TimeSpan? duration = null, bool active = false, int rssiThreshold = -80, CancellationToken cancellationToken = default)
    //{
    //    var durationMs = duration is null
    //        ? 30_000
    //        : checked((int)duration.Value.TotalMilliseconds);

    //    var parameters = new BleScanStartParams(scriptId, durationMs, active, rssiThreshold);

    //    return SendRpcAsync<BleScanStartParams, BleScanStartResult>(RpcMethods.Script_BLEScanStart, parameters, cancellationToken);
    //}

    //public ValueTask<ShellyRpcResult<BleScanStopResult>> CallBleScanStopAsync(int scriptId, CancellationToken cancellationToken = default)
    //{
    //    var parameters = new BleScanStopParams(scriptId);

    //    return SendRpcAsync<BleScanStopParams, BleScanStopResult>(RpcMethods.Script_BLEScanStop, parameters, cancellationToken);
    //}


}
