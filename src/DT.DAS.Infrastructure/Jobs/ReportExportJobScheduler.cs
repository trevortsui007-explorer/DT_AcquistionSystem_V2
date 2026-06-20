using DT.DAS.Application.Reports;
using Hangfire;

namespace DT.DAS.Infrastructure.Jobs;

public sealed class HangfireReportExportJobScheduler : IReportExportJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireReportExportJobScheduler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public string Enqueue(string taskId)
    {
        return _backgroundJobClient.Enqueue<ReportExportJob>(job => job.ExecuteAsync(taskId, CancellationToken.None));
    }
}

public sealed class NoopReportExportJobScheduler : IReportExportJobScheduler
{
    public string Enqueue(string taskId)
    {
        return "noop";
    }
}
