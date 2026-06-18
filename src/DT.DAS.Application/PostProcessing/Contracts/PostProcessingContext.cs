using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.PostProcessing.Contracts;

public sealed class PostProcessingContext
{
    public AcquisitionConfig Config { get; set; } = new();
    public string TaskLogId { get; set; } = string.Empty;
    public DateTime BusinessDate { get; set; }
    public string? SourceTableName { get; set; }
    public string? PostTableName { get; set; }
    public string? FileName { get; set; }
    public string? FullPath { get; set; }
    public IReadOnlyList<PostProcessingRowKey> Rows { get; set; } = Array.Empty<PostProcessingRowKey>();
}

public sealed class PostProcessingRowKey
{
    public Guid Id { get; set; }
    public string? FullPath { get; set; }
    public int? Row { get; set; }
}


