namespace DT.DAS.Application.Acquisition;

public interface IAcquisitionJobScheduler
{
    string Enqueue(string taskLogId, IReadOnlyCollection<int> configIds, DateTime startDate, DateTime endDate, string updateSource, bool sealOnSuccess);
}

