using System;
using System.IO;
using System.IO.Compression;
using Importer.Zip;

namespace Importer.Cli.Extensions
{
    public static class FileInfoExtensions
    {
        public static bool IsZip(this FileInfo info)
        {
            using FileStream contentStream = info.OpenRead();
            return LocalHeader.FromStream(contentStream).IsValid;
        }
    }
}