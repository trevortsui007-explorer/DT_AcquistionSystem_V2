using DT.DAS.Application.Acquisition;

namespace DT.DAS.Tests.TestDoubles;

internal sealed class RecordingScheduler : IAcquisitionJobScheduler
{
    public string? TaskLogId { get; private set; }
    public IReadOnlyCollection<int> ConfigIds { get; private set; } = Array.Empty<int>();

    public string Enqueue(string taskLogId, IReadOnlyCollection<int> configIds, DateTime startDate, DateTime endDate, string updateSource, bool sealOnSuccess)
    {
        TaskLogId = taskLogId;
        ConfigIds = configIds.ToArray();
        return "job-1";
    }
}
