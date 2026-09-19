using ShellyNET.Client;
using ShellyNET.Client.Parsers;

namespace ShellyNet.Client.UnitTests;

public sealed class ShellyConfigParserTests
{
    private static readonly ShellyComponentsConfigParser _parser = new();

    public static TheoryData<string> DeviceConfigFiles { get; } = CreateDeviceConfigFiles();

    [Theory]
    [MemberData(nameof(DeviceConfigFiles))]
    public async Task ParseConfig_ShouldParseDeviceConfig(string filePath)
    {
        var ct = TestContext.Current.CancellationToken;

        var message = await ReadStatusMessageAsync("ShellyPlusI4\\DeviceConfig-PlusI4-v.1.3.3.json", ct);
        var results = _parser.ParseConfig(message);
         
    }

    private static TheoryData<string> CreateDeviceConfigFiles()
    {
        var testFilesDirectory = Path.Combine(AppContext.BaseDirectory, "TestFiles");
        var files = Directory.EnumerateFiles(testFilesDirectory, "DeviceConfig*.json", SearchOption.AllDirectories);
        var result = new TheoryData<string>();

        foreach (var file in files)
            result.Add(file);

        return result;
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