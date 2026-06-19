using DT.DAS.Application.Configs;
using DT.DAS.Application.Configs.Contracts;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Configs.Services;

public sealed class FileConfigGroupService : IFileConfigGroupService
{
    private readonly IFileConfigGroupRepository _repository;

    public FileConfigGroupService(IFileConfigGroupRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<AcquisitionGroupDto> GetList(string? tableName = null, string? linkTableName = null, string? databaseName = null)
    {
        return _repository.GetList(tableName, linkTableName, databaseName).Select(MapGroup);
    }

    public IEnumerable<AcquisitionGroup> GetByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(ids);
        return idArray.Length == 0 ? Enumerable.Empty<AcquisitionGroup>() : _repository.GetListByIds(idArray, tableName, databaseName);
    }

    public IEnumerable<AcquisitionGroup> GetStatusByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(ids);
        if (idArray.Length == 0)
        {
            return Enumerable.Empty<AcquisitionGroup>();
        }

        return _repository.GetStatusListByIds(idArray, columnName ?? "GroupName", tableName, databaseName)
            .Select(x => new AcquisitionGroup
            {
                Id = x.Id,
                GroupName = x.Name,
                IsEnabled = x.IsEnabled
            });
    }

    public int CreateConfigGroup(AcquisitionGroup group, string? tableName = null, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(group);
        return _repository.Insert(group, tableName, databaseName);
    }

    public bool UpdateConfig(AcquisitionGroup group, string? tableName = null, string? databaseName = null)
    {
        return group.Id > 0 && _repository.Update(group, tableName, databaseName);
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

    public Task<bool> AddConfigsToGroupAsync(int groupId, IReadOnlyCollection<int> configIds, string? linkTableName = null, string? databaseName = null, CancellationToken ct = default)
    {
        if (groupId <= 0 || configIds.Count == 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_repository.AddConfigsToGroup(groupId, configIds, linkTableName, databaseName));
    }

    public bool RemoveConfigsFromGroup(int groupId, IReadOnlyCollection<int> configIds, string? linkTableName = null, string? databaseName = null)
    {
        return groupId > 0 && configIds.Count > 0 && _repository.RemoveConfigsFromGroup(groupId, configIds, linkTableName, databaseName);
    }

    private static AcquisitionGroupDto MapGroup(AcquisitionGroupListItem item)
    {
        return new AcquisitionGroupDto
        {
            Id = item.Id,
            GroupName = item.GroupName,
            GroupCategory = item.GroupCategory,
            GroupType = item.GroupType,
            ExportProcedureName = item.ExportProcedureName,
            IsEnabled = item.IsEnabled,
            ConfigCount = item.ConfigCount,
            AssociatedConfigs = item.AssociatedConfigs.Select(x => new AcquisitionConfigDto
            {
                Id = x.Id,
                EqName = x.EqName
            }).ToArray()
        };
    }

    private static string[] NormalizeIds(IEnumerable<string> ids)
    {
        return ids.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
