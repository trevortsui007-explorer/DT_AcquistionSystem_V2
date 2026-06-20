using DT.DAS.Application.Reports.Contracts;

namespace DT.DAS.Application.Reports;

public interface IReportExportService
{
    Task<ReportExportCreateResponseDto> CreateTaskAsync(ReportExportCreateRequestDto? request, CancellationToken ct = default);
    Task<ReportExportTaskDto?> GetTaskAsync(string id, CancellationToken ct = default);
    Task<ReportExportDownload> GetDownloadAsync(string id, CancellationToken ct = default);
    Task ExecuteTaskAsync(string id, CancellationToken ct = default);
}

public interface IReportExportJobScheduler
{
    string Enqueue(string taskId);
}
