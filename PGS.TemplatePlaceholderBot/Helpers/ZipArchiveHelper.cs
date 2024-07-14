using System.IO.Compression;

namespace PGS.TemplatePlaceholderBot.Helpers
{
    public class ZipArchiveHelper : IDisposable
    {
        private readonly FileStream _archiveStream;
        private readonly ZipArchive _archive;
        
        public ZipArchiveHelper(string archivePath)
        {
            _archiveStream = File.Create(archivePath);
            _archive = new ZipArchive(_archiveStream, ZipArchiveMode.Create);
        }

        public void FillZipArchive(IEnumerable<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                string fileName = filePath.Split(Path.DirectorySeparatorChar)[^1];
                _archive.CreateEntryFromFile(filePath, fileName);
            }
        }

        public void Dispose()
        {
            _archive.Dispose();
            _archiveStream.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}