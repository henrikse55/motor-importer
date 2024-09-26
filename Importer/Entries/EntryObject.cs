using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using CommunityToolkit.HighPerformance.Buffers;

using FASTER.core;

namespace Importer.Entries;

[JsonDerivedType(typeof(EntryObjectArray), typeDiscriminator: "array")]
[JsonDerivedType(typeof(EntryObjectFieldValue), typeDiscriminator: "value")]
[DebuggerDisplay("{Name} - {Key}")]
public class EntryObject
{
    public static EntryObject Create(Guid key, in ReadOnlySpan<byte> name, List<EntryObject> childFields)
    {
        var stringPool = StringPool.Shared;

        return new EntryObject()
        {
            Name = stringPool.GetOrAdd(name, Encoding.UTF8),
            Children = childFields,
            Key = key
        };
    }

    public Guid Key { get; init; }
    public string? Name { get; init; }
    public List<EntryObject>? Children { get; init; }

    public virtual void Accept(IKeyVisitor visitor)
    {
        if (Children is not null)
        {
            foreach (EntryObject child in Children)
            {
                child.Accept(visitor);
            }
        }
        visitor.Visit(this);
    }
}

[DebuggerDisplay("{Name} ({Value}) - {Key}")]
public sealed class EntryObjectFieldValue : EntryObject
{
    public static EntryObjectFieldValue Create(Guid key, in ReadOnlySpan<byte> name, in ReadOnlySpan<byte> value)
    {
        var stringPool = StringPool.Shared;

        return new EntryObjectFieldValue()
        {
            Name = stringPool.GetOrAdd(name, Encoding.UTF8),
            Value = stringPool.GetOrAdd(value, Encoding.UTF8),
            Key = key
        };
    }
    
    public string Value { get; init; }
}

[DebuggerDisplay("{Name} - {Children.Count} - {Key}")]
public sealed class EntryObjectArray : EntryObject
{
    public static EntryObjectArray CreateObject(Guid key, in ReadOnlySpan<byte> name, List<EntryObject> childFields)
    {
        var stringPool = StringPool.Shared;

        return new EntryObjectArray()
        {
            Name = stringPool.GetOrAdd(name, Encoding.UTF8),
            Children = childFields,
            Key = key
        };
    }
}
