using BenchmarkDotNet.Jobs;
using ShellyNET.Client.Model;

namespace ShellyNET.Benchmarks.Benchmarks;

// just to confirm that it is virtually free

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class MessageBenchmarks
{ 
    private readonly ShellyNotifyStatusMessage[] _messages = new ShellyNotifyStatusMessage[32];

    private readonly DateTimeOffset _timestamp =
        DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000);
     
    private readonly ComponentType _componentType = (ComponentType)1;
    private readonly PropertyName _propertyName = (PropertyName)1;

    [Benchmark(Baseline = true)]
    public ShellyNotifyStatusMessage[] CreateTen()
    {
        _messages[0] = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);
        _messages[1] = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);
        _messages[2] = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);
        _messages[3] = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);
        _messages[4] = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);
        _messages[5] = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);
        _messages[6] = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);
        _messages[7] = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);
        _messages[8] = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);
        _messages[9] = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);

        return _messages;
    }

    [Benchmark]
    public ShellyNotifyStatusMessage[] CreateOneAndCopyTen()
    {
        var message = new ShellyNotifyStatusMessage(_timestamp, 1.0, 1, _componentType, _propertyName);

        _messages[0] = message; 

        return _messages;
    }
}