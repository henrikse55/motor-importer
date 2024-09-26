using Importer.Entries;

namespace Importer;

public interface IKeyVisitor
{
    public void Visit(EntryObject entry);
}