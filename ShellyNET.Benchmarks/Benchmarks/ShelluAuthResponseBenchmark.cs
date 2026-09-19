using System.Security.Cryptography;

//| Method                       | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
//|----------------------------- |-----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
//| Hex_DirectLowercase          |   3.387 ns | 0.0355 ns | 0.0332 ns |  0.01 |    0.00 |    1 |         - |          NA |
//| NonceCount_Format_And_Copy   |   8.983 ns | 0.2035 ns | 0.2646 ns |  0.03 |    0.00 |    2 |         - |          NA |
//| Hex_Uppercase_Then_Lowercase |  14.371 ns | 0.2641 ns | 0.2470 ns |  0.05 |    0.00 |    3 |         - |          NA |
//| SHA256_Only                  | 248.092 ns | 4.9454 ns | 5.6951 ns |  0.93 |    0.03 |    4 |         - |          NA |
//| Full_DirectLowercaseHex      | 251.898 ns | 4.8496 ns | 6.3059 ns |  0.95 |    0.03 |    4 |         - |          NA |
//| Full_CurrentImplementation   | 266.505 ns | 5.3026 ns | 6.5121 ns |  1.00 |    0.03 |    5 |         - |          NA |



[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ShellyAuthResponseBenchmark
{
    private const int ModernNonceCountFieldLength = 8;
    private const int AuthenticationResponseFieldLength = SHA256.HashSizeInBytes * 2;

    private static readonly StandardFormat ModernNonceCountFormat = new('x', ModernNonceCountFieldLength);

    private byte[] _digestInput = null!;
    private byte[] _authenticationDestination = null!;
    private byte[] _hashForHexBenchmark = null!;

    private int _digestNonceCountOffset;
    private int _nonceCountOffset;
    private int _responseOffset;

    private int _nonceCount;

    [GlobalSetup]
    public void Setup()
    {
        const string realm = "shellypro4pm-f008d1d8b8b8";
        const string username = "admin";
        const string password = "benchmark-password";

        // 32-byte nonce -> 44 characters after Base64.
        // Setup is outside benchmark, so this allocation does not affect results.
        var nonce = Convert.ToBase64String(new byte[32]);

        const int cnonce = 313273957;

        // HA1 = SHA256(user:realm:password)
        var ha1Bytes = SHA256.HashData(Encoding.ASCII.GetBytes($"{username}:{realm}:{password}"));

        var ha1 = Convert.ToHexStringLower(ha1Bytes);

        // For WebSocket/RPC Shelly specifies:
        // HA2 = SHA256("dummy_method:dummy_uri")
        var ha2Bytes = SHA256.HashData("dummy_method:dummy_uri"u8);

        var ha2 = Convert.ToHexStringLower(ha2Bytes);

        // Digest input:
        //
        // <ha1>:<nonce>:<nc>:<cnonce>:auth:<ha2>
        //
        // Only <nc> changes between requests.
        var digestPrefix = $"{ha1}:{nonce}:";

        var digestSuffix = $":{cnonce}:auth:{ha2}";

        _digestNonceCountOffset = Encoding.ASCII.GetByteCount(digestPrefix);
        _digestInput = Encoding.ASCII.GetBytes(digestPrefix + "00000000" + digestSuffix);

        // Simulated already-created JSON authentication destination.
        //
        // Benchmark intentionally does not measure JsonSerializer.
        var authenticationPrefix =
            $"{{\"realm\":\"{realm}\"," +
            $"\"username\":\"{username}\"," +
            $"\"nonce\":\"{nonce}\"," +
            $"\"cnonce\":{cnonce}," +
            "\"nc\":\"";

        const string betweenNonceAndResponse = "\",\"response\":\"";

        const string authenticationSuffix = "\",\"algorithm\":\"SHA-256\"}";

        _nonceCountOffset = Encoding.ASCII.GetByteCount(authenticationPrefix);

        _responseOffset = _nonceCountOffset + ModernNonceCountFieldLength + Encoding.ASCII.GetByteCount(betweenNonceAndResponse);

        _authenticationDestination = Encoding.ASCII.GetBytes(authenticationPrefix + "00000000" + betweenNonceAndResponse + new string('0', AuthenticationResponseFieldLength) + authenticationSuffix);

        // Used by benchmarks which measure hex encoding only.
        _hashForHexBenchmark = SHA256.HashData(_digestInput);

        _nonceCount = 0;
    }

    // ------------------------------------------------------------
    // Main benchmark: implementation equivalent to the supplied code
    // ------------------------------------------------------------

    [Benchmark(Baseline = true)]
    public byte Full_CurrentImplementation()
    {
        if (_nonceCount == int.MaxValue)
        {
            throw new InvalidOperationException("The authentication nonce counter exceeded the supported range.");
        }

        var nonceCount = ++_nonceCount;

        Span<byte> nonceCountBytes = stackalloc byte[ModernNonceCountFieldLength];

        if (!Utf8Formatter.TryFormat(nonceCount, nonceCountBytes, out var nonceCountBytesWritten, ModernNonceCountFormat) || nonceCountBytesWritten != ModernNonceCountFieldLength)
        {
            throw new InvalidOperationException("The authentication nonce counter did not fit the reserved field.");
        }

        nonceCountBytes.CopyTo(_digestInput.AsSpan(_digestNonceCountOffset, ModernNonceCountFieldLength));
        nonceCountBytes.CopyTo(_authenticationDestination.AsSpan(_nonceCountOffset, ModernNonceCountFieldLength));
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(_digestInput, hash);

        WriteLowerHex(hash, _authenticationDestination.AsSpan(_responseOffset, AuthenticationResponseFieldLength));

        // Gives BenchmarkDotNet an observable result.
        return _authenticationDestination[_responseOffset];
    }

    // ------------------------------------------------------------
    // .NET 10 alternative:
    // avoids uppercase HEX + second lowercase pass
    // ------------------------------------------------------------

    [Benchmark]
    public byte Full_DirectLowercaseHex()
    {
        if (_nonceCount == int.MaxValue)
        {
            throw new InvalidOperationException();
        }

        var nonceCount = ++_nonceCount;

        Span<byte> nonceCountBytes = stackalloc byte[ModernNonceCountFieldLength];

        if (!Utf8Formatter.TryFormat(nonceCount, nonceCountBytes, out var nonceCountBytesWritten, ModernNonceCountFormat) || nonceCountBytesWritten != ModernNonceCountFieldLength)
        {
            throw new InvalidOperationException();
        }

        nonceCountBytes.CopyTo(_digestInput.AsSpan(_digestNonceCountOffset, ModernNonceCountFieldLength));
        nonceCountBytes.CopyTo(_authenticationDestination.AsSpan(_nonceCountOffset, ModernNonceCountFieldLength));
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(_digestInput, hash);
        WriteLowerHexDirect(hash, _authenticationDestination.AsSpan(_responseOffset, AuthenticationResponseFieldLength));

        return _authenticationDestination[_responseOffset];
    }

    // ------------------------------------------------------------
    // Diagnostic benchmarks
    // ------------------------------------------------------------

    [Benchmark]
    public byte NonceCount_Format_And_Copy()
    {
        var nonceCount = ++_nonceCount;

        Span<byte> nonceCountBytes = stackalloc byte[ModernNonceCountFieldLength];

        if (!Utf8Formatter.TryFormat(nonceCount, nonceCountBytes, out var bytesWritten, ModernNonceCountFormat) ||
            bytesWritten != ModernNonceCountFieldLength)
        {
            throw new InvalidOperationException();
        }

        nonceCountBytes.CopyTo(_digestInput.AsSpan(_digestNonceCountOffset, ModernNonceCountFieldLength));
        nonceCountBytes.CopyTo(_authenticationDestination.AsSpan(_nonceCountOffset, ModernNonceCountFieldLength));

        return nonceCountBytes[0];
    }

    [Benchmark]
    public byte SHA256_Only()
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(_digestInput, hash);

        return hash[0];
    }

    [Benchmark]
    public byte Hex_Uppercase_Then_Lowercase()
    {
        WriteLowerHex(_hashForHexBenchmark, _authenticationDestination.AsSpan(_responseOffset, AuthenticationResponseFieldLength));

        return _authenticationDestination[_responseOffset];
    }

    [Benchmark]
    public byte Hex_DirectLowercase()
    {
        WriteLowerHexDirect(_hashForHexBenchmark, _authenticationDestination.AsSpan(_responseOffset, AuthenticationResponseFieldLength));

        return _authenticationDestination[_responseOffset];
    }

    // ------------------------------------------------------------
    // Current implementation
    // ------------------------------------------------------------

    private static void WriteLowerHex(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        var converted = Convert.TryToHexString(source, destination, out var bytesWritten) &&
            bytesWritten == destination.Length;

        if (!converted) throw new InvalidOperationException("The hexadecimal digest did not fit the reserved destination.");

        var status = Ascii.ToLowerInPlace(destination, out var bytesLowercased);

        if (status != OperationStatus.Done || bytesLowercased != destination.Length)
            throw new InvalidOperationException("The hexadecimal digest could not be normalized to lowercase ASCII.");
    }

    // ------------------------------------------------------------
    // .NET 10 version
    // ------------------------------------------------------------

    private static void WriteLowerHexDirect(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (!Convert.TryToHexStringLower(source, destination, out var bytesWritten) || bytesWritten != destination.Length)
        {
            throw new InvalidOperationException("The hexadecimal digest did not fit the reserved destination.");
        }
    }
}