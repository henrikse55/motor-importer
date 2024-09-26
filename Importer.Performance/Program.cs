using System.Diagnostics;
using Importer.Performance;
using Importer.Zip;

await using var file = File.OpenRead("/storage/motor/data.zip");
using var zipStream = new StreamableZipFile(file);
await using var fileStream = zipStream.GetStream();

long startTimeStamp = Stopwatch.GetTimestamp();

Console.WriteLine("Starting Load of XML Data...");
PerformanceReader reader = new PerformanceReader(CancellationToken.None);

await reader.Read(fileStream);

Console.WriteLine($"Finished of XML Data in {Stopwatch.GetElapsedTime(startTimeStamp)} seconds.");
