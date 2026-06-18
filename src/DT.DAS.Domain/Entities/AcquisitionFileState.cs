namespace DT.DAS.Domain.Entities;

public sealed class AcquisitionFileState
{
    public long Id { get; set; }
    public int ConfigId { get; set; }
    public DateTime BusinessDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FullPath { get; set; }
    public int DataRowCount { get; set; }
    public int LastStartRow { get; set; }
    public int LastProcessedRows { get; set; }
    public string? LastTaskLogId { get; set; }
    public string? LastStatus { get; set; }
    public string? LastUpdateSource { get; set; }
    public bool IsSealed { get; set; }
    public DateTime? SealTime { get; set; }
    public DateTime? LastScanTime { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

