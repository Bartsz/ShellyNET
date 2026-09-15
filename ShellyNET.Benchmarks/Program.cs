using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;

namespace ShellyNET.Benchmarks;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);

        var isLinuxArm64 = OperatingSystem.IsLinux();

        IConfig config = DefaultConfig.Instance
            //.AddJob(Job.Default
            //    .WithRuntime(NativeAotRuntime.Net10_0))
            ;

        if (isLinuxArm64)
        {
            config = ManualConfig
                .Create(DefaultConfig.Instance)
                .AddJob(
                    Job.Default
                        .WithToolchain(InProcessEmitToolchain.Instance)
                        .WithId("Linux-Arm64-InProcess"));
        }

        switcher.Run(args, config);
    }
}
