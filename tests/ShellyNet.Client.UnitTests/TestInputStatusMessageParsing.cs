using ShellyNET.Client.Parsers;

namespace ShellyNet.Client.UnitTests;

public sealed class TestCombinedFullSytheticMessageParsing
{
    private const long StatusTimestampMilliseconds = 123_456_120;
    private readonly ShellyNotifyStatusParser _parser = new();
    

    /// <summary>Verifies that a standard NotifyStatus input state change is parsed and enqueued correctly.</summary>
    [Fact(Timeout = 500)]
    public async Task ShouldParseFullSyntheticMessagesInOrder()
    {
        var ct = TestContext.Current.CancellationToken;

        var message = await ReadStatusMessageAsync("NotifyStatus\\combined-full-synthetic.json", ct);
        var results = _parser.ProcessNotifyStatusMessage(message, StatesContainingObject.Params, ct);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(StatusTimestampMilliseconds);
        var expectedMessage = new ShellyNotifyStatusMessage(timestamp, true, 1, ComponentType.Input, PropertyName.State);

    }
    

    /// <summary>Verifies that a standard NotifyStatus input state change is parsed and enqueued correctly.</summary>
    [Fact(Timeout = 500)]
    public async Task ShouldParsePlusI4RealStatus()
    {
        var ct = TestContext.Current.CancellationToken;

        var message = await ReadStatusMessageAsync("ShellyPlusI4\\DeviceStatus-PlusI4-v1.3.3.json", ct);
        var results = _parser.ProcessNotifyStatusMessage(message, StatesContainingObject.Results, ct);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(StatusTimestampMilliseconds);
        var expectedMessage = new ShellyNotifyStatusMessage(timestamp, true, 1, ComponentType.Input, PropertyName.State);

    }

    /// <summary>Verifies that properties inside a NotifyStatus component object can be parsed independently of their order.</summary>
    [Fact(Timeout = 500)]
    public async Task ProcessNotifyStatusMessage_OutOfOrderComponentProperties_EnqueuesExpectedStateChange()
    {
        var ct = TestContext.Current.CancellationToken;

        var message = await ReadStatusMessageAsync("NotifyStatus\\Input\\input-change-out-of-order-object-v.1.0.7.json", ct);
        var results = _parser.ProcessNotifyStatusMessage(message, StatesContainingObject.Params, ct);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(StatusTimestampMilliseconds);
        var expectedMessage = new ShellyNotifyStatusMessage(timestamp, true, 1, ComponentType.Input, PropertyName.State);

        Assert.Equal(expectedMessage, results.MessageSpan.ToArray()[0]);
    }

    /// <summary>Verifies that NotifyStatus message properties can be parsed independently of their order.</summary>
    [Fact(Timeout = 500)]
    public async Task ProcessNotifyStatusMessage_OutOfOrderMessageProperties_EnqueuesExpectedStateChange()
    {
        var ct = TestContext.Current.CancellationToken;
         
        var message = await ReadStatusMessageAsync("NotifyStatus\\Input\\input-change-out-of-order-params-v.1.0.7.json", ct);
        var results = _parser.ProcessNotifyStatusMessage(message, StatesContainingObject.Params, ct);

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(StatusTimestampMilliseconds);
        var expectedMessage = new ShellyNotifyStatusMessage(timestamp, true, 1, ComponentType.Input, PropertyName.State);

        Assert.Equal(expectedMessage, results.MessageSpan.ToArray()[0]);
    }

    /// <summary>Verifies that NotifyStatus message properties can be parsed independently of their order.</summary>
    [Fact(Timeout = 500)]
    public async Task ProcessNotifyStatusMessage_InputFullState_EnqueuesExpectedStateChange()
    {
        var ct = TestContext.Current.CancellationToken;

        var message = await ReadStatusMessageAsync("NotifyStatus\\Input\\input-full-change-v.1.0.7.json", ct);
        var results = _parser.ProcessNotifyStatusMessage(message, StatesContainingObject.Params, ct);

        Assert.Equal(11, results.MessageCount);
    }

    /// <summary>Verifies that NotifyStatus message properties can be parsed independently of their order.</summary>
    [Fact(Timeout = 500)]
    public async Task ProcessNotifyStatusMessage_SwitchFullState_EnqueuesExpectedStateChange()
    {
        var ct = TestContext.Current.CancellationToken;

        var message = await ReadStatusMessageAsync("NotifyStatus\\Switch\\switch-full-change-v.1.0.7.json", ct);
        var results = _parser.ProcessNotifyStatusMessage(message, StatesContainingObject.Params, ct);

        Assert.Equal(16, results.MessageCount);
    }
     
    /// <summary>Reads a NotifyStatus payload copied to the test output directory.</summary>
    private static async Task<Memory<byte>> ReadStatusMessageAsync(string fileName, CancellationToken cancellationToken)
    {
        var statusDirectory = Path.Combine(AppContext.BaseDirectory, "TestFiles");
        var filePath = Path.Combine(statusDirectory, fileName);

        Assert.True(File.Exists(filePath), $"Test status file does not exist: {filePath}");

        var messageBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        Memory<byte> message = messageBytes;

        return message;
    }
}