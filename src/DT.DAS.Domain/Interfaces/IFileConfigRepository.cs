using DT.DAS.Domain.Entities;

namespace DT.DAS.Domain.Interfaces;

public interface IFileConfigRepository
{
    IEnumerable<AcquisitionConfig> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfig> GetListByGroupIds(IEnumerable<string> groupIds, string? tableName = null, string? linkTableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfig> GetListByTaskIds(IEnumerable<string> taskIds, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfig> GetList(FileConfigQueryOptions? options = null);
}

