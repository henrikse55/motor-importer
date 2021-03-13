namespace Importer.Cli.Extensions
{
    public static class StringExtensions
    {
        public static (string username, string password) GetMongoGetCredentials(this string combined, char delimiter = ',')
        {
            string[] separatedCreds = combined.Split(delimiter);
            return (separatedCreds[0], separatedCreds[1]);
        }
    }
}