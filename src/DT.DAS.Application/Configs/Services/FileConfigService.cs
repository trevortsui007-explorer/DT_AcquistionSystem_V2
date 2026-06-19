using DT.DAS.Application.Configs;
using DT.DAS.Application.Configs.Contracts;
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

    public PagedResult<AcquisitionConfig> GetFileConfigsPaged(FileConfigQueryOptions? options, int page, int limit)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedLimit = limit <= 0 ? 10 : limit;
        var result = _repository.GetPageList(options ?? new FileConfigQueryOptions(), normalizedPage, normalizedLimit, options?.TableName, options?.DatabaseName);

        return new PagedResult<AcquisitionConfig>
        {
            Total = result.Total,
            List = result.List
        };
    }

    public IEnumerable<AcquisitionConfig> GetFileConfigs(FileConfigQueryOptions? options)
    {
        options ??= new FileConfigQueryOptions();

        if (options.HasTaskFilter)
        {
            return GetConfigsByTaskIds(options.TaskIds!, options.TableName, options.DatabaseName);
        }

        if (options.HasGroupFilter)
        {
            return GetConfigsByGroupIds(options.GroupIds!, options.TableName, options.LinkTableName, options.DatabaseName);
        }

        if (options.HasIdFilter)
        {
            return GetByIds(options.Ids!, options.TableName, options.DatabaseName);
        }

        return _repository.GetList(options);
    }

    public IEnumerable<AcquisitionConfig> GetByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(ids);
        return idArray.Length == 0 ? Enumerable.Empty<AcquisitionConfig>() : _repository.GetListByIds(idArray, tableName, databaseName);
    }

    public IEnumerable<AcquisitionConfig> GetConfigsByGroupIds(IEnumerable<string> groupIds, string? tableName = null, string? linkTableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(groupIds);
        return idArray.Length == 0 ? Enumerable.Empty<AcquisitionConfig>() : _repository.GetListByGroupIds(idArray, tableName, linkTableName, databaseName);
    }

    public IEnumerable<AcquisitionConfig> GetConfigsByTaskIds(IEnumerable<string> taskIds, string? tableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(taskIds);
        return idArray.Length == 0 ? Enumerable.Empty<AcquisitionConfig>() : _repository.GetListByTaskIds(idArray, tableName, databaseName);
    }

    public IEnumerable<AcquisitionConfigStatus> GetStatusByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(ids);
        return idArray.Length == 0 ? Enumerable.Empty<AcquisitionConfigStatus>() : _repository.GetStatusListByIds(idArray, columnName, tableName, databaseName);
    }

    public int CreateConfig(AcquisitionConfig config, string? tableName = null, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        return _repository.Insert(config, tableName, databaseName);
    }

    public bool UpdateConfig(AcquisitionConfig config, string? tableName = null, string? databaseName = null)
    {
        return config.Id > 0 && _repository.Update(config, tableName, databaseName);
    }

    public bool DeleteConfigs(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(ids);
        return idArray.Length > 0 && _repository.Delete(idArray, tableName, databaseName);
    }

    public bool SetEnabledStatus(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(ids);
        return idArray.Length > 0 && _repository.SetEnabled(idArray, isEnabled, tableName, databaseName);
    }

    private static string[] NormalizeIds(IEnumerable<string> ids)
    {
        return ids.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
