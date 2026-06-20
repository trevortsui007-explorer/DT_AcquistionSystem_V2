using System.IO.Compression;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Infrastructure.Export;

public sealed class ReportArchiveService : IReportArchiveService
{
    public void CreateZip(string zipPath, IEnumerable<string> filePaths)
    {
        var directory = Path.GetDirectoryName(zipPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var filePath in filePaths)
        {
            archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath), CompressionLevel.Fastest);
        }
    }
}
