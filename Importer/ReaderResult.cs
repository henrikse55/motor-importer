using System;

namespace Importer
{
    public readonly struct ReaderResult
    {
        public readonly byte[] Content;

        public ReaderResult(byte[] content)
        {
            Content = content;
        }
    }
}