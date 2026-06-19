using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Tests.TestDoubles;

internal class InMemoryFileConfigRepository : IFileConfigRepository
{
    private readonly List<AcquisitionConfig> _configs;

    public InMemoryFileConfigRepository(params AcquisitionConfig[] configs)
    {
        _configs = configs.ToList();
    }

    public IEnumerable<AcquisitionConfig> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        var idSet = ids.Select(int.Parse).ToHashSet();
        return _configs.Where(x => idSet.Contains(x.Id));
    }

    public IEnumerable<AcquisitionConfig> GetListByGroupIds(IEnumerable<string> groupIds, string? tableName = null, string? linkTableName = null, string? databaseName = null) => _configs;
    public IEnumerable<AcquisitionConfig> GetListByTaskIds(IEnumerable<string> taskIds, string? tableName = null, string? databaseName = null) => _configs;
    public IEnumerable<AcquisitionConfig> GetList(FileConfigQueryOptions? options = null) => _configs;
    public (int Total, IReadOnlyCollection<AcquisitionConfig> List) GetPageList(FileConfigQueryOptions options, int page, int limit, string? tableName = null, string? databaseName = null) => (_configs.Count, _configs);
    public IEnumerable<AcquisitionConfigStatus> GetStatusListByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null) => _configs.Select(x => new AcquisitionConfigStatus { Id = x.Id, Name = x.EqName, IsEnabled = x.IsEnabled });
    public int Insert(AcquisitionConfig config, string? tableName = null, string? databaseName = null) { config.Id = _configs.Count + 1; _configs.Add(config); return config.Id; }
    public bool Update(AcquisitionConfig config, string? tableName = null, string? databaseName = null) => config.Id > 0;
    public bool Delete(IEnumerable<string> ids, string? tableName = null, string? databaseName = null) => ids.Any();
    public bool SetEnabled(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null) => ids.Any();
}

internal sealed class RecordingFileConfigRepository : IFileConfigRepository
{
    private readonly IReadOnlyCollection<AcquisitionConfig> _configs;
    public string? LastReadRoute { get; private set; }
    public (int Page, int Limit) LastPageRequest { get; private set; }

    public RecordingFileConfigRepository(params AcquisitionConfig[] configs)
    {
        _configs = configs;
    }

    public (int Total, IReadOnlyCollection<AcquisitionConfig> List) GetPageList(FileConfigQueryOptions options, int page, int limit, string? tableName = null, string? databaseName = null)
    {
        LastPageRequest = (page, limit);
        return (_configs.Count, _configs);
    }

    public IEnumerable<AcquisitionConfig> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null) { LastReadRoute = "ids"; return _configs; }
    public IEnumerable<AcquisitionConfig> GetListByGroupIds(IEnumerable<string> groupIds, string? tableName = null, string? linkTableName = null, string? databaseName = null) { LastReadRoute = "groups"; return _configs; }
    public IEnumerable<AcquisitionConfig> GetListByTaskIds(IEnumerable<string> taskIds, string? tableName = null, string? databaseName = null) { LastReadRoute = "tasks"; return _configs; }
    public IEnumerable<AcquisitionConfig> GetList(FileConfigQueryOptions? options = null) { LastReadRoute = "all"; return _configs; }
    public IEnumerable<AcquisitionConfigStatus> GetStatusListByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null) => Array.Empty<AcquisitionConfigStatus>();
    public int Insert(AcquisitionConfig config, string? tableName = null, string? databaseName = null) => 1;
    public bool Update(AcquisitionConfig config, string? tableName = null, string? databaseName = null) => true;
    public bool Delete(IEnumerable<string> ids, string? tableName = null, string? databaseName = null) => true;
    public bool SetEnabled(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null) => true;
}

internal sealed class InMemoryFileConfigGroupRepository : IFileConfigGroupRepository
{
    public IEnumerable<AcquisitionGroupListItem> GetList(string? tableName = null, string? linkTableName = null, string? databaseName = null)
    {
        return new[]
        {
            new AcquisitionGroupListItem
            {
                Id = 1,
                GroupName = "G-1",
                IsEnabled = true,
                ConfigCount = 1,
                AssociatedConfigs = new[] { new AcquisitionConfigSummary { Id = 1, EqName = "EQ-1" } }
            }
        };
    }

    public IEnumerable<AcquisitionGroup> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null) => new[] { new AcquisitionGroup { Id = 1, GroupName = "G-1", IsEnabled = true } };
    public IEnumerable<AcquisitionConfigStatus> GetStatusListByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null) => new[] { new AcquisitionConfigStatus { Id = 1, Name = "G-1", IsEnabled = true } };
    public int Insert(AcquisitionGroup group, string? tableName = null, string? databaseName = null) => 1;
    public bool Update(AcquisitionGroup group, string? tableName = null, string? databaseName = null) => true;
    public bool Delete(IEnumerable<string> ids, string? tableName = null, string? databaseName = null) => ids.Any();
    public bool SetEnabled(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null) => ids.Any();
    public bool AddConfigsToGroup(int groupId, IReadOnlyCollection<int> configIds, string? linkTableName = null, string? databaseName = null) => groupId > 0 && configIds.Count > 0;
    public bool RemoveConfigsFromGroup(int groupId, IReadOnlyCollection<int> configIds, string? linkTableName = null, string? databaseName = null) => groupId > 0 && configIds.Count > 0;
}
