using DT.DAS.Domain.Entities;

namespace DT.DAS.Domain.Interfaces;

public interface IFileConfigRepository
{
    (int Total, IReadOnlyCollection<AcquisitionConfig> List) GetPageList(FileConfigQueryOptions options, int page, int limit, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfig> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfig> GetListByGroupIds(IEnumerable<string> groupIds, string? tableName = null, string? linkTableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfig> GetListByTaskIds(IEnumerable<string> taskIds, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfig> GetList(FileConfigQueryOptions? options = null);
    IEnumerable<AcquisitionConfigStatus> GetStatusListByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null);
    int Insert(AcquisitionConfig config, string? tableName = null, string? databaseName = null);
    bool Update(AcquisitionConfig config, string? tableName = null, string? databaseName = null);
    bool Delete(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    bool SetEnabled(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null);
}
