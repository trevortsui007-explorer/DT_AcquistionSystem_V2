namespace DT.DAS.Tests.WebApi;

public sealed class ProjectStructureTests
{
    [Fact]
    public void Project_structure_uses_module_folders_and_sql_scripts()
    {
        var root = FindSolutionRoot();

        Assert.True(Directory.Exists(Path.Combine(root, "src", "DT.DAS.Application", "Acquisition")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "DT.DAS.Application", "Configs")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "DT.DAS.Application", "Tasks")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "DT.DAS.Application", "PostProcessing")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "DT.DAS.WebApi", "Modules", "Tasks")));
        Assert.True(Directory.Exists(Path.Combine(root, "Tests", "DT.DAS.Tests", "Acquisition")));
        Assert.True(Directory.Exists(Path.Combine(root, "Tests", "DT.DAS.Tests", "Configs")));
        Assert.True(Directory.Exists(Path.Combine(root, "Tests", "DT.DAS.Tests", "Tasks")));
        Assert.True(Directory.Exists(Path.Combine(root, "Tests", "DT.DAS.Tests", "Infrastructure")));
        Assert.True(Directory.Exists(Path.Combine(root, "Tests", "DT.DAS.Tests", "TestDoubles")));
        Assert.True(File.Exists(Path.Combine(root, "src", "DT.DAS.Infrastructure", "Persistence", "Scripts", "Tasks", "001_create_acquisition_task_logs.sql")));
        Assert.False(File.Exists(Path.Combine(root, "Tests", "DT.DAS.Tests", "CoreInfrastructureTests.cs")));
    }

    private static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "DT_DataAcquisitionSystem.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException("Could not find solution root.");
    }
}
