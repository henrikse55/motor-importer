using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Importer.Utility
{
    public static class SpanUtility
    {
        public static int[] FindAllDelimiterIndexes(ReadOnlySpan<byte> content, ReadOnlySpan<byte> delimiter)
        {
            List<int> indexes = new List<int>();
            while (true)
            {
                int index = content.IndexOf(delimiter);
                if (index == -1)
                    break;

                indexes.Add(index);
                
                content = content.Slice(index + delimiter.Length - 1);
            }

            return indexes.ToArray();
        }
    }
}