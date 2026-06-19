namespace DT.DAS.Infrastructure.Options;

public sealed class DasDatabaseOptions
{
    public string DefaultConnectionName { get; set; } = "BaseDb";
    public string ConfigTableName { get; set; } = "DA_AcquisitionConfig";
    public string GroupTableName { get; set; } = "DA_AcquisitionGroup";
    public string GroupConfigTableName { get; set; } = "DA_AcquisitionGroup_Config";
    public string TaskGroupTableName { get; set; } = "DA_AcquisitionTask_Group";
    public string TaskTableName { get; set; } = "DA_AcquisitionTask";
}

public sealed class HangfireOptions
{
    public bool Enabled { get; set; }
    public string ConnectionName { get; set; } = "BaseDb";
}
