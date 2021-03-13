using System.Text;

namespace Importer
{
    public static class Constants
    {
        public static string EndingTag = "</ns:Statistik>";

        public static byte[] EndingTagBytes { get; } = Encoding.UTF8.GetBytes(EndingTag);
        
        public static string StartTag = "<ns:Statistik>";

        public static byte[] StartTagBytes { get; } = Encoding.UTF8.GetBytes(StartTag);
    }
}