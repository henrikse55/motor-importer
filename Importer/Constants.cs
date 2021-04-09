using System.Text;

namespace Importer
{
    public static class Constants
    {
        public const string EndingTag = "</ns:Statistik>";
        public static byte[] EndingTagBytes { get; } = Encoding.UTF8.GetBytes(EndingTag);

        
        public const string StartTag = "<ns:Statistik>";
        public static byte[] StartTagBytes { get; } = Encoding.UTF8.GetBytes(StartTag);

        
        public const string NameSpaceDelimiter = "ns:";
        public static byte[] NameSpaceDelimiterBytes { get; } = Encoding.UTF8.GetBytes(NameSpaceDelimiter);
    }
}