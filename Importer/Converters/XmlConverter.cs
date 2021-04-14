using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Importer.Utility;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Toolkit.HighPerformance.Buffers;


namespace Importer.Converters
{
    public partial class XmlConverter
    {
        private string PatchXmlData(ReadOnlySpan<byte> content)
        {
            using MemoryOwner<byte> fixedXml = ApplyXmlFix(content);
            return StringUtility.GetXmlWithoutNamespacesFromBytes(fixedXml);
        }

        private MemoryOwner<byte> ApplyXmlFix(ReadOnlySpan<byte> content)
        {
            content = FindStartOfXml(content);
            MemoryOwner<byte> expanded = AddEndTag(content);
            return expanded;
        }

        private ReadOnlySpan<byte> FindStartOfXml(ReadOnlySpan<byte> content)
        {
            int index = content.IndexOf(Constants.StartTagBytes);
            return content.Slice(index);
        }
        
        private MemoryOwner<byte> AddEndTag(ReadOnlySpan<byte> content)
        {
            int finalLength = content.Length + Constants.EndingTagBytes.Length;
            MemoryOwner<byte> finalXmlContent = MemoryOwner<byte>.Allocate(finalLength);
            
            content.CopyTo(finalXmlContent.Span);
            Constants.EndingTagBytes.CopyTo(finalXmlContent.Span.Slice(content.Length));
            return finalXmlContent;
        }

        private string GetXmlHashId(MemoryOwner<byte> item)
        {
            byte[] hashResult = SHA512.HashData(item.Span);
            return string.Create(hashResult.Length, hashResult, (state, bytes) =>
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    ref byte b = ref bytes[i];
                    builder.Append(b.ToString("X2"));
                }
                builder.CopyTo(0, state, state.Length);
            });
        }
    }
}