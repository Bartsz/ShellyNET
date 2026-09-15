using System.Buffers;
using System.Text.Unicode;

//BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
//12th Gen Intel Core i7-1280P 2.00GHz, 1 CPU, 20 logical and 14 physical cores
//.NET SDK 11.0.100-preview.3.26207.106
//  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
//  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

//| Method                      | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
//|---------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
//| Utf8TryWrite                | 3.2009 ns | 0.0898 ns | 0.1135 ns | 3.1845 ns |  1.00 |    0.05 |      - |         - |          NA |
//| TryFormatAndPad             | 1.5150 ns | 0.0309 ns | 0.0274 ns | 1.5145 ns |  0.47 |    0.02 |      - |         - |          NA |
//| CopyTemplateToSpan          | 4.8147 ns | 0.1285 ns | 0.3666 ns | 4.6854 ns |  1.51 |    0.13 |      - |         - |          NA |
//| CopyTemplateToArrayPool     | 9.2065 ns | 0.1908 ns | 0.2041 ns | 9.2041 ns |  2.88 |    0.12 |      - |         - |          NA |
//| CopyTemplateToNewArray      | 8.2747 ns | 0.2762 ns | 0.7968 ns | 7.9221 ns |  2.59 |    0.26 | 0.0070 |      88 B |          NA |
//| CopyTemplateToArrayPoolLoop | 0.7040 ns | 0.0138 ns | 0.0179 ns | 0.6987 ns |  0.22 |    0.01 |      - |         - |          NA |


[MemoryDiagnoser]
public class FormatBenchmark
{
    private const ushort Value = 423;
    private readonly byte[] msgTemplate = """{"id":1    ,"source":"_","method":"Shelly.GetDeviceInfo"}"""u8.ToArray();
    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    [Benchmark(Baseline = true)]
    public int Utf8TryWrite()
    {
        Span<byte> buffer = stackalloc byte[5];
        Utf8.TryWrite(buffer, CultureInfo.InvariantCulture, $"{Value,-5}", out int written);
        return written;
    }

    [Benchmark]
    public int TryFormatAndPad()
    {
        Span<byte> buffer = stackalloc byte[5];
        Value.TryFormat(buffer, out int written);
        buffer[written..].Fill((byte)' ');
        return 5;
    }

    [Benchmark]
    public int CopyTemplateToSpan()
    {
        Span<byte> buffer = stackalloc byte[msgTemplate.Length];
        msgTemplate.CopyTo(buffer);
        Value.TryFormat(buffer, out int written);
        return written;
    }

    [Benchmark]
    public int CopyTemplateToArrayPool()
    {
        var buffer = _pool.Rent(128);
        msgTemplate.CopyTo(buffer);
        Value.TryFormat(buffer, out int written);
        _pool.Return(buffer);
        return written;
    }

    [Benchmark]
    public byte[] CopyTemplateToNewArray()
    {
        var buffer = new byte[msgTemplate.Length];
        msgTemplate.CopyTo(buffer);
        Value.TryFormat(buffer, out int written);
        return buffer;
    }

    [Benchmark(OperationsPerInvoke = 1000)]
    public int CopyTemplateToArrayPoolLoop()
    {
        var total = 0;
        for (int i = 0; i < 100; i++)
        {
            var buffer = _pool.Rent(128);
            msgTemplate.CopyTo(buffer);
            total.TryFormat(buffer, out int written);
            _pool.Return(buffer);
            total += written;
        }
        return total;
    }
}