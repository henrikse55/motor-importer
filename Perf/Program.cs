using System;
using System.Linq;
using BenchmarkDotNet.Running;

namespace Perf;

sealed class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<StringUtilsBench>();
        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}