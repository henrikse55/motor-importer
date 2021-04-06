using System;
using System.IO;
using System.Net;

namespace Importer.Zip
{
    public class RemoteFile : IDisposable
    {
        private readonly Uri _downloadUri;
        private StreamableZipFile _zipFile;

        public static Stream GetRemoteFileAsStream(string uri)
            => new RemoteFile(uri).GetStreamingFile();

        public RemoteFile(string uri) : this(new Uri(uri))
        {
            
        }
        
        public RemoteFile(Uri downloadUri)
        {
            _downloadUri = downloadUri;
        }

        public Stream GetStreamingFile()
        {
            Stream ftpZipStream = DownloadRemote();
            _zipFile = new StreamableZipFile(ftpZipStream);
            return _zipFile.GetStream();
        }
        
        private Stream DownloadRemote()
        {
            Console.WriteLine($"Starting Download for {_downloadUri}");
            FtpWebRequest request = MakeFtpRequest();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Console.WriteLine($"Got response code: {response.StatusCode} | {response.StatusDescription ?? ""}");

            return response.GetResponseStream();
        }

        private FtpWebRequest MakeFtpRequest()
        {
            FtpWebRequest request = (FtpWebRequest) WebRequest.Create(_downloadUri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            return request;
        }

        public void Dispose()
        {
            _zipFile.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}