using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Importer;
using Importer.Converters;
using Importer.Utility;
using Microsoft.Toolkit.HighPerformance.Buffers;
using MongoDB.Bson;

namespace Perf
{
    [MemoryDiagnoser]
    [SimpleJob]
    public class XmlConversion
    {
        public byte[] ContentBytes = Encoding.UTF8.GetBytes(LargeContent.LargeXmlEntry);
        public MemoryOwner<byte> ProcessItem;

        [GlobalSetup]
        public void StartUp()
        {
            ProcessItem = MemoryOwner<byte>.Allocate(ContentBytes.Length);
            ((Span<byte>)ContentBytes).CopyTo(ProcessItem.Span);
            
        }
        
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Convert")]
        public BsonDocument ConvertToJsonToBson()
        { 
            return BsonDocument.Parse(XmlConverter.ConvertToJson(ProcessItem));
        }
        
        [Benchmark]
        [BenchmarkCategory("Convert")]
        public BsonDocument ConvertToBson()
        {
            return new XmlConverter().ConvertToBson(ProcessItem);
        }
    }
}