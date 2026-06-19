using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Files.Contracts;

public sealed class FileDiscoveryDto
{
    public string? MonthName { get; set; }
    public string? FolderPath { get; set; }
    public int FileCount => Files.Count;
    public List<FileEntryDto> Files { get; set; } = new();
}

public sealed class FileEntryDto
{
    public string? FileName { get; set; }
    public string? FullPath { get; set; }
    public DateTime DetectedDate { get; set; }
    public bool IsMissing { get; set; }
    public int? DataRowCount { get; set; }
    public int? LastStartRow { get; set; }
    public int? LastProcessedRows { get; set; }
    public string? LastStatus { get; set; }
    public string? LastUpdateSource { get; set; }
    public bool? IsSealed { get; set; }
    public DateTime? LastScanTime { get; set; }
    public DateTime? FileStateUpdateTime { get; set; }
}

public sealed class GroupFileDiscoveryDto
{
    public int GroupId { get; set; }
    public DateTime Date { get; set; }
    public List<GroupFileDiscoveryItemDto> Items { get; set; } = new();
}

public sealed class GroupFileDiscoveryItemDto
{
    public int ConfigId { get; set; }
    public string? EqName { get; set; }
    public bool IsMissing { get; set; }
    public string? FullFilePath { get; set; }
}

public sealed class FileUploadResult
{
    public string? Path { get; set; }
    public string? FileName { get; set; }
    public long Size { get; set; }
}

public sealed class FileDownloadResult
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
}
