using System;
using System.IO;
using System.Text;
using Importer.Zip;

namespace Importer.Extensions
{
    public static class StreamExtensions
    {
        public static string GetFileNameFromZipStream(this Stream stream, LocalHeader header)
        {
            Span<byte> buffer = stackalloc byte[header.FileNameLenght];
            stream.Read(buffer);
            return Encoding.UTF8.GetString(buffer);
        }
        
        public static void SkipExtraField(this Stream stream, LocalHeader header)
        {
            if (header.ExtraLenght != 0)
            {
                Span<byte> buffer = stackalloc byte[header.ExtraLenght];
                stream.Read(buffer);
            }
        }
    }
}