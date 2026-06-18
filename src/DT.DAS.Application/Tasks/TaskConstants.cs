namespace DT.DAS.Application.Tasks;

public static class TaskTriggerTypes
{
    public const string Manual = "MAN";
    public const string Scheduled = "SCH";
}

public static class FileStateUpdateSources
{
    public const string ScheduledCurrent = "SCH_CURRENT";
    public const string ScheduledD1Backfill = "SCH_D1_BACKFILL";
    public const string ManualCurrent = "MAN_CURRENT";
    public const string ManualRepair = "MAN_REPAIR";
}


