using System.IO.Compression;

namespace Importer.Zip;

public sealed class StreamableZipFile : IDisposable
{
    private readonly Stream _data;
    private readonly LocalHeader _header;
    public string FileName { get; }

    public StreamableZipFile(Stream data)
    {
        _data = data;
        _header = LocalHeader.FromStream(data);

        FileName = GetFileNameFromStream();
        data.SkipExtraField(_header);
    }

    private string GetFileNameFromStream()
    {
        return _data.GetFileNameFromZipStream(_header);
    }

    public Stream GetStream() 
        => new DeflateStream(_data, CompressionMode.Decompress, false);

    public void Dispose()
    {
        _data?.Dispose();
    }
}