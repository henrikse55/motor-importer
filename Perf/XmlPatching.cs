using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Importer;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Perf
{
    [SimpleJob]
    [MemoryDiagnoser]
    public class XmlPatching
    {
        private readonly byte[] _contentBytes = Encoding.UTF8.GetBytes(LargeContent.LargeXmlEntry);
        private MemoryOwner<byte> _processItem;

        [GlobalSetup]
        public void StartUp()
        {
            _processItem = MemoryOwner<byte>.Allocate(_contentBytes.Length);
            ((Span<byte>) _contentBytes).CopyTo(_processItem.Span);
        }

        [Benchmark(Baseline = true)]
        public ReadOnlySpan<byte> ApplyXmlFix()
        {
            ReadOnlySpan<byte> content = _processItem.Span;

            int index = content.IndexOf(Constants.StartTagBytes);
            content = content.Slice(index);

            int finalLength = content.Length + Constants.EndingTagBytes.Length;
            MemoryOwner<byte> finalXmlContent = MemoryOwner<byte>.Allocate(finalLength);

            content.CopyTo(finalXmlContent.Span);
            Constants.EndingTagBytes.CopyTo(finalXmlContent.Span.Slice(content.Length));
            return finalXmlContent.Span;
        }

        [Benchmark]
        public ReadOnlySpan<byte> BufferedPatchXml()
        {
            ReadOnlySpan<byte> content = _processItem.Span;

            int index = content.IndexOf(Constants.StartTagBytes);
            content = content.Slice(index);

            ArrayPoolBufferWriter<byte> fixedXml = new(content.Length + Constants.EndingTagBytes.Length);
            fixedXml.Write(content);
            fixedXml.Write((ReadOnlySpan<byte>) Constants.EndingTagBytes);

            return fixedXml.WrittenSpan;
        }
    }
}