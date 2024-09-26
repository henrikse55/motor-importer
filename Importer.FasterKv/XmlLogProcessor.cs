using System.Buffers;
using System.Collections.Frozen;
using System.Threading.Tasks.Dataflow;
using FASTER.core;
using Importer.Converters;
using Importer.Entries;
using Importer.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Importer.FasterKv;

public sealed class XmlLogProcessor
{
    private readonly ILogger<XmlLogProcessor> _logger;
    private readonly DbContextOptions<ApplicationContext> _options;
    private readonly HashSet<Guid> _existingIds = new();
    private readonly XmlLogStorage _storage;
    private readonly XmlConverter _converter;

    private readonly BatchBlock<MotorEntry> _batchBlock;
    private readonly ActionBlock<MotorEntry[]> _uploadBlock;

    public XmlLogProcessor(
        XmlLogStorage storage,
        XmlConverter converter,
        ILogger<XmlLogProcessor> logger,
        DbContextOptions<ApplicationContext> options)
    {
        _storage = storage;
        _converter = converter;
        _logger = logger;
        _options = options;
        
        _batchBlock = new BatchBlock<MotorEntry>(2048);
        _uploadBlock = new ActionBlock<MotorEntry[]>(Action, new ExecutionDataflowBlockOptions{MaxDegreeOfParallelism = 16});

        
        _batchBlock.LinkTo(_uploadBlock, new DataflowLinkOptions(){PropagateCompletion = true});
    }
    
    private async Task Action(MotorEntry[] obj)
    {
        await using ApplicationContext context = new(_options);

        await context.MotorEntries.AddRangeAsync(obj).ConfigureAwait(false);
        int total = await context.SaveChangesAsync().ConfigureAwait(false);
        _logger.LogInformation("Wrote a total of {Rows}", total);
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuredAsyncEnum = _storage.ReadCommittedXmlContent(stoppingToken);
        await foreach ((byte[]? entry, int length) in configuredAsyncEnum.ConfigureAwait(false))
        {
            var withoutNameSpaces = StringUtility.GetXmlWithoutNamespacesToMemory(new ReadOnlySequence<byte>(entry));
            var patchedXmlData =  await XmlConverter.PatchXmlDataAsync(withoutNameSpaces).ConfigureAwait(false);
            var item = _converter.ConvertToEntry(patchedXmlData);
            
            await InsertEntry(item).ConfigureAwait(false);
            MotorMeter.Log.ReportEntryPatched(entry.Length);
        }
        
        _batchBlock.TriggerBatch();
        _batchBlock.Complete();
        await _uploadBlock.Completion.ConfigureAwait(false);
    }

    private async Task InsertEntry(EntryObject entryObject)
    {
        try
        {
            var motorEntries = CreateEntriesFromNestedObjects(entryObject).ToList();
            
            foreach (var motor in motorEntries)
            {
                if(_existingIds.Add(motor.Key))
                    await _batchBlock.SendAsync(motor).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not save the entry entity to the database due to an error being thrown");
            throw;
        }
    }

    private IEnumerable<MotorEntry> CreateEntriesFromNestedObjects(EntryObject entryObject)
    {
        if (entryObject is EntryObjectFieldValue)
            return [];

        if (_existingIds.Contains(entryObject.Key))
            return [];
        
        var toBecomeRelations = entryObject.Children!
            .Where(x => x is not EntryObjectFieldValue or EntryObjectArray)
            .ToLookup(x => x.Name)
            .Where(x => x.Count() == 1)
            .Select(x => x.First())
            .ToDictionary(x => x.Name!, x => x.Key.ToString());

        var arrayRelations = entryObject.Children!
            .ToLookup(x => x.Name)
            .Where(x => x.Count() > 1)
            .ToDictionary(x => x.Key!, 
                x => string.Join(",", x.SelectMany(p => p.Children!).Select(c => c.Key.ToString())));
        
        MotorEntry entry = new()
        {
            Key = entryObject.Key,
            ObjectName = entryObject.Name!,
            Fields = entryObject.Children!
                .Where(x => x is EntryObjectFieldValue)
                .Cast<EntryObjectFieldValue>()
                .ToDictionary(x => x.Name!, x => x.Value),
            Relations = toBecomeRelations,
            RelatedArrays = arrayRelations
        };

        List<MotorEntry> nestedEntries = new();
        foreach (EntryObject becomeRelation in entryObject.Children.Where(x => x.Children != null))
        {
            nestedEntries.AddRange(CreateEntriesFromNestedObjects(becomeRelation));
        }

        return [entry, ..nestedEntries];
    }
}