using ShellyNET.Client;
using ShellyNET.Client.Model;

namespace ShellyNet.Client.UnitTests;

public sealed class ShellyClientNotifyEventTests
{
    private const long EventTimestampMilliseconds = 1_789_469_256_980;

    /// <summary>Verifies that a standard NotifyEvent message produces the expected Shelly event.</summary>
    [Fact(Timeout = 5_000)]
    public async Task ProcessNotifyEventMessage_StandardEvent_EnqueuesExpectedEvent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = CreateShellyClient();

        var message = await ReadEventMessageAsync("event_input_btn_up.json", cancellationToken);

        client.ProcessNotifyEventMessage(message, cancellationToken);

        var actualEvent = await client.GetShellyEvent(cancellationToken);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(EventTimestampMilliseconds);
        var expectedEvent = new ShellyComponentEvent(timestamp, 0, ComponentType.Input, ComponentEvent.BtnUp);

        Assert.Equal(expectedEvent.Id, actualEvent.Id);
        Assert.Equal(expectedEvent.Timestamp.ToUnixTimeMilliseconds(), actualEvent.Timestamp.ToUnixTimeMilliseconds());
        Assert.Equal(expectedEvent.ComponentEvent, actualEvent.ComponentEvent);
        Assert.Equal(expectedEvent.ComponentType, actualEvent.ComponentType);
        Assert.Equivalent(expectedEvent, actualEvent);
    }

    /// <summary>Verifies that event properties can be parsed independently of their order in the JSON object.</summary>
    [Fact(Timeout = 5_000)]
    public async Task ProcessNotifyEventMessage_OutOfOrderProperties_EnqueuesExpectedEvents()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = CreateShellyClient();

        var message = await ReadEventMessageAsync("out_of_order_properties.json", cancellationToken);

        client.ProcessNotifyEventMessage(message, cancellationToken);

        var firstActualEvent = await client.GetShellyEvent(cancellationToken);
        var secondActualEvent = await client.GetShellyEvent(cancellationToken);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(EventTimestampMilliseconds);
        var firstExpectedEvent = new ShellyComponentEvent(timestamp, 0, ComponentType.Input, ComponentEvent.BtnUp);
        var secondExpectedEvent = new ShellyComponentEvent(timestamp, 0, ComponentType.Input, ComponentEvent.BtnDown);

        Assert.Equal(firstExpectedEvent, firstActualEvent);
        Assert.Equal(secondExpectedEvent, secondActualEvent);
    }

    /// <summary>Verifies that all events from a single NotifyEvent message are enqueued in their original order.</summary>
    [Fact(Timeout = 5_000)]
    public async Task ProcessNotifyEventMessage_MultipleEvents_EnqueuesAllEventsInOrder()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = CreateShellyClient();

        var message = await ReadEventMessageAsync("two_events.json", cancellationToken);

        client.ProcessNotifyEventMessage(message, cancellationToken);

        var firstActualEvent = await client.GetShellyEvent(cancellationToken);
        var secondActualEvent = await client.GetShellyEvent(cancellationToken);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(EventTimestampMilliseconds);
        var firstExpectedEvent = new ShellyComponentEvent(timestamp, 0, ComponentType.Input, ComponentEvent.BtnUp);
        var secondExpectedEvent = new ShellyComponentEvent(timestamp, 0, ComponentType.Input, ComponentEvent.BtnDown);

        Assert.Equal(firstExpectedEvent, firstActualEvent);
        Assert.Equal(secondExpectedEvent, secondActualEvent);
    }

    /// <summary>Creates an isolated Shelly client so queued events cannot leak between tests.</summary>
    private static ShellyClient CreateShellyClient()
    {
        var client = new ShellyClient();
        return client;
    }

    /// <summary>Reads an event payload copied to the test output directory.</summary>
    private static async Task<Memory<byte>> ReadEventMessageAsync(string fileName, CancellationToken cancellationToken)
    {
        var eventsDirectory = Path.Combine(AppContext.BaseDirectory, "TestFiles", "Events");
        var filePath = Path.Combine(eventsDirectory, fileName);

        Assert.True(File.Exists(filePath), $"Test event file does not exist: {filePath}");

        var messageBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        Memory<byte> message = messageBytes;

        return message;
    }
}