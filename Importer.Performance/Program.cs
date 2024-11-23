using System.Diagnostics;
using FASTER.core;
using Importer.Performance;

await using var file = File.OpenRead("/storage/motor/data.xml");
await using var bufferedFile = new BufferedStream(file);
// using var zipStream = new StreamableZipFile(file);
// await using var fileStream = zipStream.GetStream();

FasterLogSettings config = new ("/storage/motor/log");
FasterLog log = new FasterLog(config);

long startTimeStamp = Stopwatch.GetTimestamp();

Console.WriteLine("Starting Load of XML Data...");
PerformanceReader reader = new PerformanceReader(log, CancellationToken.None);
long itemsParsed = await reader.Read(file);

Console.WriteLine($"Finished of XML Data in {Stopwatch.GetElapsedTime(startTimeStamp)} seconds finding {itemsParsed} Xml Documents");
