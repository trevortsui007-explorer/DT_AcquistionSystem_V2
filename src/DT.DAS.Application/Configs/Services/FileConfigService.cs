using DT.DAS.Application.Configs;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Configs.Services;

public sealed class FileConfigService : IFileConfigService
{
    private readonly IFileConfigRepository _repository;

    public FileConfigService(IFileConfigRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<AcquisitionConfig> GetByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        return _repository.GetListByIds(ids, tableName, databaseName);
    }

    public IEnumerable<AcquisitionConfig> GetConfigsByGroupIds(IEnumerable<string> groupIds)
    {
        return _repository.GetListByGroupIds(groupIds);
    }

    public IEnumerable<AcquisitionConfig> GetConfigsByTaskIds(IEnumerable<string> taskIds)
    {
        return _repository.GetListByTaskIds(taskIds);
    }

    public IEnumerable<AcquisitionConfig> GetFileConfigs(FileConfigQueryOptions? options)
    {
        return _repository.GetList(options);
    }
}



