using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Importer.Converters;
using Microsoft.Toolkit.HighPerformance.Buffers;
using MongoDB.Bson;

namespace Perf
{
    [SimpleJob]
    [MemoryDiagnoser]
    public class XmlConversion
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
        [BenchmarkCategory("Convert")]
        public BsonDocument ConvertToJsonToBson()
        {
            return BsonDocument.Parse(XmlConverter.ConvertToJson(_processItem));
        }

        [Benchmark]
        [BenchmarkCategory("Convert")]
        public BsonDocument ConvertToBson()
        {
            return new XmlConverter().ConvertToBson(_processItem);
        }
    }
}