using System;
using System.IO;
using System.IO.Compression;

namespace Importer.Cli.Extensions
{
    public static class FileInfoExtensions
    {
        public static bool IsZip(this FileInfo info)
        {
            try
            {
                using Stream fileStream = info.OpenRead();
                using ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}