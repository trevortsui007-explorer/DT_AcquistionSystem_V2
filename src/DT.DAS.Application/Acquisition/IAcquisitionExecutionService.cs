using DT.DAS.Application.Acquisition.Contracts;

namespace DT.DAS.Application.Acquisition;

public interface IAcquisitionExecutionService
{
    Task<TaskStartResponseDto> StartByIdsAsync(string[] ids, DateTime processDate, CancellationToken ct = default);
    Task<TaskStartResponseDto> StartByGroupsAsync(string[] groupIds, DateTime processDate, CancellationToken ct = default);
    Task<TaskStartResponseDto> StartByTasksAsync(string[] taskIds, DateTime processDate, CancellationToken ct = default);
    Task<TaskStatusDto?> GetTaskStatusAsync(string taskLogId, CancellationToken ct = default);
    Task<List<TaskDetailLogDto>> GetTaskDetailsAsync(string taskLogId, CancellationToken ct = default);
}

