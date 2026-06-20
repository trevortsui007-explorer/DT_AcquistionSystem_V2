using System.Data;
using System.IO.Compression;
using DT.DAS.Infrastructure.Export;
using DT.DAS.Infrastructure.Persistence;
using DT.DAS.Infrastructure.Persistence.Repositories;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Tests.Reports;

public sealed class ReportExportInfrastructureTests
{
    [Fact]
    public async Task ReportExportDataProvider_rejects_unsafe_procedure_name_before_opening_connection()
    {
        var provider = new ReportExportDataProvider(new ThrowingSqlConnectionFactory());

        await Assert.ThrowsAsync<ArgumentException>(() => provider.ExecuteGroupReportAsync(1, "dbo.Export;DROP", DateTime.Today, DateTime.Today));
    }

    [Fact]
    public void NpoiReportWorkbookWriter_writes_xlsx_and_sanitizes_sheet_names()
    {
        var path = Path.Combine(Path.GetTempPath(), "dt-das-report-" + Guid.NewGuid().ToString("N") + ".xlsx");
        try
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add("alpha");
            var writer = new NpoiReportWorkbookWriter();

            writer.Write(path, new[] { new ReportDataSet { SheetName = "Sheet/Name:*?ThatIsWayTooLongForExcel", Data = table } });

            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
            Assert.DoesNotContain('/', NpoiReportWorkbookWriter.SanitizeSheetName("Bad/Name"));
            Assert.True(NpoiReportWorkbookWriter.SanitizeSheetName(new string('a', 40)).Length <= 31);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ReportArchiveService_creates_zip()
    {
        var root = Path.Combine(Path.GetTempPath(), "dt-das-report-zip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "a.txt");
        var zip = Path.Combine(root, "out.zip");
        File.WriteAllText(file, "hello");

        try
        {
            new ReportArchiveService().CreateZip(zip, new[] { file });

            Assert.True(File.Exists(zip));
            using var archive = ZipFile.OpenRead(zip);
            Assert.Contains(archive.Entries, x => x.Name == "a.txt");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private sealed class ThrowingSqlConnectionFactory : ISqlConnectionFactory
    {
        public Microsoft.Data.SqlClient.SqlConnection Create(string? name = null)
        {
            throw new InvalidOperationException("connection should not be opened");
        }
    }
}

