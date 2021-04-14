using System;
using System.Linq;
using BenchmarkDotNet.Running;

namespace Perf
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}