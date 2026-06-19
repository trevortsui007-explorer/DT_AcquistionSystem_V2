using DT.DAS.Application.Tasks.Services;
using DT.DAS.Domain.Entities;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Tasks;

public sealed class TaskServiceTests
{
    [Fact]
    public void AcquisitionTaskService_guards_empty_mutations()
    {
        var repository = new RecordingAcquisitionTaskRepository();
        var service = new AcquisitionTaskService(repository);

        Assert.False(service.DeleteTasks(Array.Empty<string>()));
        Assert.False(service.SetEnabledStatus(Array.Empty<string>(), true));
    }

    [Fact]
    public void AcquisitionTaskService_sets_create_and_update_timestamps()
    {
        var repository = new RecordingAcquisitionTaskRepository();
        var service = new AcquisitionTaskService(repository);
        var task = new AcquisitionTask { TaskName = "T-New", TaskMode = 1, IsEnabled = true };

        var id = service.CreateTask(task);

        Assert.Equal(9, id);
        Assert.NotEqual(default, repository.InsertedTask!.CreateTime);
        Assert.NotEqual(default, repository.InsertedTask.UpdateTime);

        var beforeUpdate = DateTime.Now.AddSeconds(-1);
        var success = service.UpdateTask(new AcquisitionTask { Id = 1, TaskName = "T-Updated", TaskMode = 2 });

        Assert.True(success);
        Assert.True(repository.UpdatedTask!.UpdateTime >= beforeUpdate);
    }

    [Fact]
    public void AcquisitionTaskService_assigns_groups_by_replacing_existing_links()
    {
        var repository = new RecordingAcquisitionTaskRepository();
        var service = new AcquisitionTaskService(repository);

        var success = service.AssignGroupsToTask(1, new[] { 2, 2, 0, -1, 3 });

        Assert.True(success);
        Assert.True(repository.RemoveAllGroupsCalled);
        Assert.Equal(new[] { 2, 3 }, repository.AddedGroupIds);
    }

    [Fact]
    public void AcquisitionTaskService_empty_group_assignment_clears_existing_links()
    {
        var repository = new RecordingAcquisitionTaskRepository();
        var service = new AcquisitionTaskService(repository);

        var success = service.AssignGroupsToTask(1, Array.Empty<int>());

        Assert.True(success);
        Assert.True(repository.RemoveAllGroupsCalled);
        Assert.Empty(repository.AddedGroupIds);
    }
}
