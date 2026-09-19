using ShellyNET.Client.Parsers;

namespace ShellyNET.Benchmarks.Benchmarks;

//BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
//12th Gen Intel Core i7-1280P 2.00GHz, 1 CPU, 20 logical and 14 physical cores
//.NET SDK 11.0.100-preview.3.26207.106
//  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
//  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


//| Method                            | Mean       | Error    | StdDev   | Allocated |
//|---------------------------------- |-----------:|---------:|---------:|----------:|
//| ParseSwitchOutputChangeMessage    |   251.0 ns |  3.53 ns |  3.30 ns |         - |
//| ParseSwitchFullStateChangeMessage | 1,152.9 ns | 22.94 ns | 44.75 ns |         - |
//| ParseInputFullStateChangeMessage  |   726.2 ns |  5.86 ns |  4.89 ns |         - |
//| ParseEventMessage                 |   267.0 ns |  2.59 ns |  2.30 ns |         - |


[MemoryDiagnoser]
public class ShellyStatusParserBenchmark
{
    private byte[] _eventJson;
    private byte[] _switchOutputChangeJson;
    private ShellyNotifyStatusParser _statusParser = new();
    private ShellyNotifyEventParser _eventParser = new();
    private static CancellationToken ct = CancellationToken.None;
    private byte[] _switchFullChangeJson;
    private byte[] _inputFullChangeJson;

    [GlobalSetup]
    public void Setup()
    {
        _statusParser = new();

        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "event_input_btn_up.json");
        _eventJson = File.ReadAllBytes(path);

        path = Path.Combine(AppContext.BaseDirectory, "TestData", "switch-output-change-v.1.0.7.json");
        _switchOutputChangeJson = File.ReadAllBytes(path);

        path = Path.Combine(AppContext.BaseDirectory, "TestData", "switch-full-change-v.1.0.7.json");
        _switchFullChangeJson = File.ReadAllBytes(path);

        path = Path.Combine(AppContext.BaseDirectory, "TestData", "input-full-change-v.1.0.7.json");
        _inputFullChangeJson = File.ReadAllBytes(path);
    }

    [Benchmark]
    public int ParseSwitchOutputChangeMessage()
    {
        var result = _statusParser.ProcessNotifyStatusMessage(_switchOutputChangeJson.AsMemory(), StatesContainingObject.Params, ct);
        result.Dispose();
        return result.MessageCount;
    }

    [Benchmark]
    public int ParseSwitchFullStateChangeMessage()
    {
        var result = _statusParser.ProcessNotifyStatusMessage(_switchFullChangeJson.AsMemory(), StatesContainingObject.Params, ct);
        result.Dispose();
        return result.MessageCount;
    }

    [Benchmark]
    public int ParseInputFullStateChangeMessage()
    {
        var result = _statusParser.ProcessNotifyStatusMessage(_inputFullChangeJson.AsMemory(), StatesContainingObject.Params, ct);
        result.Dispose();
        return result.MessageCount;
    }

    [Benchmark]
    public int ParseEventMessage()
    {
        var result = _eventParser.ProcessNotifyEventMessage(_eventJson.AsMemory(), ct);
        result.Dispose();
        return result.EventsCount;
    }
}