using ShellyNET.Client;

namespace ShellyNET.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ShellyClientParsingBenchmark
{
    private byte[] _eventJson;
    private byte[] _switchOutputChangeJson;
    private ShellyClient _client = new();
    private static CancellationToken ct = CancellationToken.None;
    private byte[] _switchFullChangeJson;

    [GlobalSetup]
    public void Setup()
    {
        _client = new();

        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "event_input_btn_up.json");
        _eventJson = File.ReadAllBytes(path);

        path = Path.Combine(AppContext.BaseDirectory, "TestData", "switch-output-change-v.1.0.7.json");
        _switchOutputChangeJson = File.ReadAllBytes(path);

        path = Path.Combine(AppContext.BaseDirectory, "TestData", "switch-full-change-v.1.0.7.json");
        _switchFullChangeJson = File.ReadAllBytes(path);
    }


    [Benchmark]
    public void ParseSwitchOutputChangeMessage()
    {
        _client.ProcessNotifyStatusMessage(_switchOutputChangeJson.AsMemory(), ct);
        //await _client.GetShellyEvent(ct);
    }

    /// <summary> 128 is the channel size limit so abouve this it will no tbe ralistic.</summary>
    [Benchmark(OperationsPerInvoke = 128)]
    public void ParseSwitchOutputChangeMessageLoop()
    {
        for (int i = 0; i < 128; i++)
        {
            _client.ProcessNotifyStatusMessage(_switchOutputChangeJson.AsMemory(), ct);
        }
        //await _client.GetShellyEvent(ct);
    }

    [Benchmark]
    public void ParseSwitchFullStateChangeMessage()
    {
        _client.ProcessNotifyStatusMessage(_switchFullChangeJson.AsMemory(), ct);
        //await _client.GetShellyEvent(ct);
    }


    [Benchmark]
    public void ParseEventMessage()
    {
        _client.ProcessNotifyEventMessage(_eventJson.AsMemory(), ct);
        //await _client.GetShellyEvent(ct);
    }

}
