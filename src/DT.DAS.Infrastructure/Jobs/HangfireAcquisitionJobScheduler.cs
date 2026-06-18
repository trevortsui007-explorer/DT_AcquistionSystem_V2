using DT.DAS.Application.Acquisition;
using DT.DAS.Application.Configs;
using DT.DAS.Application.PostProcessing;
using DT.DAS.Application.PostProcessing.Contracts;
using DT.DAS.Application.Tasks;
using Hangfire;

namespace DT.DAS.Infrastructure.Jobs;

public sealed class HangfireAcquisitionJobScheduler : IAcquisitionJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireAcquisitionJobScheduler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public string Enqueue(string taskLogId, IReadOnlyCollection<int> configIds, DateTime startDate, DateTime endDate, string updateSource, bool sealOnSuccess)
    {
        return _backgroundJobClient.Enqueue<AcquisitionJob>(job => job.ExecuteAsync(configIds.ToArray(), startDate, endDate, taskLogId, updateSource, sealOnSuccess, CancellationToken.None));
    }
}

public sealed class NoopAcquisitionJobScheduler : IAcquisitionJobScheduler
{
    public string Enqueue(string taskLogId, IReadOnlyCollection<int> configIds, DateTime startDate, DateTime endDate, string updateSource, bool sealOnSuccess)
    {
        return "noop";
    }
}

