using ShellyNET.Client;

namespace ShellyNet.Client.UnitTests;

public sealed class ShellyConfigParserTests
{
    private static readonly ShellyComponentsConfigParser Parser = new();

    public static TheoryData<string> DeviceConfigFiles { get; } = CreateDeviceConfigFiles();

    [Theory]
    [MemberData(nameof(DeviceConfigFiles))]
    public void ParseConfig_ShouldParseDeviceConfig(string filePath)
    {
        var payload = File.ReadAllBytes(filePath);

        var components = Parser.ParseConfig(payload);
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
}