using ShellyNET.Client;

namespace NidoWeaver.Benchmark.Benchmarks.JsonBenchmarks;


//BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
//12th Gen Intel Core i7-1280P 2.00GHz, 1 CPU, 20 logical and 14 physical cores
//.NET SDK 11.0.100-preview.3.26207.106
//  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
//  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

//| Method                | Mean     | Error    | StdDev   | Gen0   | Allocated |
//|---------------------- |---------:|---------:|---------:|-------:|----------:|
//| ParseComponentsCofnig | 10.17 us | 0.196 us | 0.281 us | 0.5188 |   6.45 KB |
 

[MemoryDiagnoser]
public class ShellyParsingComponentsConfig
{
    private byte[] _json = null!;
    private readonly ShellyComponentsConfigParser _parser = new();

    [GlobalSetup]
    public void Setup()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "DeviceConfig-1.0.7-raw.json");
        _json = File.ReadAllBytes(path);
    }

    [Benchmark]
    public void ParseComponentsCofnig()
    {
        _parser.ParseConfig(_json.AsMemory());  
    }
}

