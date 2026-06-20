using DT.DAS.Application.Reports;

namespace DT.DAS.Infrastructure.Jobs;

public sealed class ReportExportJob
{
    private readonly IReportExportService _reportExportService;

    public ReportExportJob(IReportExportService reportExportService)
    {
        _reportExportService = reportExportService;
    }

    public Task ExecuteAsync(string taskId, CancellationToken ct = default)
    {
        return _reportExportService.ExecuteTaskAsync(taskId, ct);
    }
}
