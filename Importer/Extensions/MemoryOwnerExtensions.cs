using System.Security.Cryptography;
using System.Text;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Importer.Extensions
{
    public static class MemoryOwnerExtensions
    {
        public static string GetByteHash(this MemoryOwner<byte> item)
        {
            byte[] hashResult = SHA512.HashData(item.Span);
            return string.Create(hashResult.Length, hashResult, (state, bytes) =>
            {
                StringBuilder builder = new();
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