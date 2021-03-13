using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Importer.Zip
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 30)]
    public struct LocalHeader
    {
        public bool IsValid => Magic == 0x04034b50;

        public int Magic;

        public short Version;

        public short Flags;

        public short Compression;

        public short ModTime;

        public short ModDate;

        public int Crc;

        public int CompressSize;

        public int UnCompressSize;

        public short FileNameLenght;

        public short ExtraLenght;

        public static LocalHeader FromBytes(Span<byte> buffer) 
            => MemoryMarshal.Read<LocalHeader>(buffer);

        public static LocalHeader FromStream(Stream stream)
        {
            Span<byte> buffer = new byte[30];
            int bytesRead = stream.Read(buffer);

            if (bytesRead != 30)
                throw new InvalidOperationException($"read too little of stream: {bytesRead} should be 30");

            return FromBytes(buffer);
        }
    }

}