using System.Data;
using DT.DAS.Domain.Entities;

namespace DT.DAS.Domain.Interfaces;

public interface IReportExportTaskRepository
{
    Task EnsureStorageAsync(CancellationToken ct = default);
    Task CreateAsync(ReportExportTask task, CancellationToken ct = default);
    Task<ReportExportTask?> GetByIdAsync(string id, CancellationToken ct = default);
    Task UpdateProgressAsync(string id, string status, int progress, string stage, string? errorMessage = null, CancellationToken ct = default);
    Task CompleteAsync(string id, string filePath, string fileName, CancellationToken ct = default);
    Task FailAsync(string id, string errorMessage, CancellationToken ct = default);
}

public sealed class ReportExportGroupDefinition
{
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? ExportProcedureName { get; set; }
}

public sealed class ReportDataSet
{
    public string SheetName { get; set; } = "Sheet1";
    public DataTable Data { get; set; } = new();
}

public interface IReportExportDataProvider
{
    Task<IList<ReportExportGroupDefinition>> GetGroupDefinitionsAsync(IEnumerable<int> groupIds, CancellationToken ct = default);
    Task<IList<ReportDataSet>> ExecuteGroupReportAsync(int groupId, string procedureName, DateTime startTime, DateTime endTime, CancellationToken ct = default);
}

public interface IReportWorkbookWriter
{
    void Write(string filePath, IList<ReportDataSet> dataSets);
}

public interface IReportArchiveService
{
    void CreateZip(string zipPath, IEnumerable<string> filePaths);
}
