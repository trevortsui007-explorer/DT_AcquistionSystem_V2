using DT.DAS.Application.Acquisition;
using DT.DAS.Application.Configs;
using DT.DAS.Application.PostProcessing;
using DT.DAS.Application.PostProcessing.Contracts;
using DT.DAS.Application.Tasks;
using DT.DAS.Domain.Entities;

namespace DT.DAS.Infrastructure.Jobs;

public sealed class AcquisitionJob
{
    private readonly IFileConfigService _fileConfigService;
    private readonly IDataAcquisitionService _dataAcquisitionService;

    public AcquisitionJob(IFileConfigService fileConfigService, IDataAcquisitionService dataAcquisitionService)
    {
        _fileConfigService = fileConfigService;
        _dataAcquisitionService = dataAcquisitionService;
    }

    public Task ExecuteAsync(int[] configIds, DateTime startDate, DateTime endDate, string taskLogId, string updateSource, bool sealOnSuccess, CancellationToken ct = default)
    {
        var configs = _fileConfigService.GetByIds(configIds.Select(x => x.ToString())).ToList();
        return _dataAcquisitionService.ExecuteBatchWithTaskLogAsync(configs, startDate, endDate, taskLogId, ct, updateSource, sealOnSuccess);
    }
}

