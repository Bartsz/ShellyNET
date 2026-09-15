namespace ShellyNet.Client.UnitTests;

public sealed class TestInputStatusMessageParsing
{
    private const long StatusTimestampMilliseconds = 1_789_468_476_400;

    /// <summary>Verifies that a standard NotifyStatus input state change is parsed and enqueued correctly.</summary>
    [Fact(Timeout = 5_000)]
    public async Task ProcessNotifyStatusMessage_StandardMessage_EnqueuesExpectedStateChange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = CreateShellyClient();

        var message = await ReadStatusMessageAsync("input-change-v.1.0.7.json", cancellationToken);

        client.ProcessNotifyStatusMessage(message, cancellationToken);

        var actualMessage = await client.GetShellyStatusMessage(cancellationToken);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(StatusTimestampMilliseconds);
        var expectedMessage = new ShellyStateChangeMsg(timestamp, 1, 1, ComponentType.Input, PropertyName.State);

        Assert.Equal(expectedMessage, actualMessage);
    }

    /// <summary>Verifies that properties inside a NotifyStatus component object can be parsed independently of their order.</summary>
    [Fact(Timeout = 5_000)]
    public async Task ProcessNotifyStatusMessage_OutOfOrderComponentProperties_EnqueuesExpectedStateChange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = CreateShellyClient();

        var message = await ReadStatusMessageAsync("input-change-out-of-order-object-v.1.0.7.json", cancellationToken);

        client.ProcessNotifyStatusMessage(message, cancellationToken);

        var actualMessage = await client.GetShellyStatusMessage(cancellationToken);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(StatusTimestampMilliseconds);
        var expectedMessage = new ShellyStateChangeMsg(timestamp, 1, 1, ComponentType.Input, PropertyName.State);

        Assert.Equal(expectedMessage, actualMessage);
    }

    /// <summary>Verifies that NotifyStatus message properties can be parsed independently of their order.</summary>
    [Fact(Timeout = 5_000)]
    public async Task ProcessNotifyStatusMessage_OutOfOrderMessageProperties_EnqueuesExpectedStateChange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = CreateShellyClient();

        var message = await ReadStatusMessageAsync("input-change-out-of-order-params-v.1.0.7.json", cancellationToken);

        client.ProcessNotifyStatusMessage(message, cancellationToken);

        var actualMessage = await client.GetShellyStatusMessage(cancellationToken);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(StatusTimestampMilliseconds);
        var expectedMessage = new ShellyStateChangeMsg(timestamp, 1, 1, ComponentType.Input, PropertyName.State);

        Assert.Equal(expectedMessage, actualMessage);
    }

    /// <summary>Creates an isolated Shelly client so queued status messages cannot leak between tests.</summary>
    private static ShellyClient CreateShellyClient()
    {
        var client = new ShellyClient();
        return client;
    }

    /// <summary>Reads a NotifyStatus payload copied to the test output directory.</summary>
    private static async Task<Memory<byte>> ReadStatusMessageAsync(string fileName, CancellationToken cancellationToken)
    {
        var statusDirectory = Path.Combine(AppContext.BaseDirectory, "TestFiles", "Status");
        var filePath = Path.Combine(statusDirectory, fileName);

        Assert.True(File.Exists(filePath), $"Test status file does not exist: {filePath}");

        var messageBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        Memory<byte> message = messageBytes;

        return message;
    }
}