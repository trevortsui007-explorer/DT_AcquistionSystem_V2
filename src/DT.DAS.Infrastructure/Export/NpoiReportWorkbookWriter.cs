using System.Data;
using DT.DAS.Domain.Interfaces;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming;

namespace DT.DAS.Infrastructure.Export;

public sealed class NpoiReportWorkbookWriter : IReportWorkbookWriter
{
    private const int MaxDataRowsPerSheet = 999999;

    public void Write(string filePath, IList<ReportDataSet> dataSets)
    {
        if (dataSets == null || dataSets.Count == 0)
        {
            throw new InvalidOperationException("没有可导出的数据集。");
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var workbook = new SXSSFWorkbook(100);
        var headerStyle = workbook.CreateCellStyle();
        var headerFont = workbook.CreateFont();
        headerFont.IsBold = true;
        headerStyle.SetFont(headerFont);

        var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dataSet in dataSets)
        {
            WriteDataSet(workbook, headerStyle, dataSet, usedSheetNames);
        }

        using var stream = new FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
        workbook.Write(stream);
        workbook.Dispose();
    }

    private static void WriteDataSet(SXSSFWorkbook workbook, ICellStyle headerStyle, ReportDataSet dataSet, HashSet<string> usedSheetNames)
    {
        var table = dataSet.Data ?? new DataTable();
        var totalRows = table.Rows.Count;
        var sheetCount = Math.Max(1, (int)Math.Ceiling(totalRows / (double)MaxDataRowsPerSheet));

        for (var partIndex = 0; partIndex < sheetCount; partIndex++)
        {
            var baseName = sheetCount == 1 ? dataSet.SheetName : $"{dataSet.SheetName}_{partIndex + 1}";
            var sheetName = CreateUniqueSheetName(baseName, usedSheetNames);
            var sheet = workbook.CreateSheet(sheetName);
            sheet.CreateFreezePane(0, 1);
            WriteHeader(sheet, headerStyle, table);

            var start = partIndex * MaxDataRowsPerSheet;
            var end = Math.Min(totalRows, start + MaxDataRowsPerSheet);
            for (var rowIndex = start; rowIndex < end; rowIndex++)
            {
                var sourceRow = table.Rows[rowIndex];
                var row = sheet.CreateRow(rowIndex - start + 1);
                for (var colIndex = 0; colIndex < table.Columns.Count; colIndex++)
                {
                    WriteCell(row.CreateCell(colIndex), sourceRow[colIndex]);
                }
            }
        }
    }

    private static void WriteHeader(ISheet sheet, ICellStyle headerStyle, DataTable table)
    {
        var header = sheet.CreateRow(0);
        for (var i = 0; i < table.Columns.Count; i++)
        {
            var cell = header.CreateCell(i);
            cell.SetCellValue(table.Columns[i].ColumnName);
            cell.CellStyle = headerStyle;
        }
    }

    private static void WriteCell(ICell cell, object? value)
    {
        if (value == null || value == DBNull.Value)
        {
            cell.SetCellValue(string.Empty);
            return;
        }

        switch (value)
        {
            case DateTime dateTime:
                cell.SetCellValue(dateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                break;
            case bool boolean:
                cell.SetCellValue(boolean);
                break;
            case int or long or short or decimal or double or float:
                if (double.TryParse(Convert.ToString(value), out var number)) cell.SetCellValue(number);
                else cell.SetCellValue(Convert.ToString(value));
                break;
            default:
                cell.SetCellValue(Convert.ToString(value));
                break;
        }
    }

    private static string CreateUniqueSheetName(string requestedName, HashSet<string> usedSheetNames)
    {
        var clean = SanitizeSheetName(requestedName);
        var candidate = clean;
        var index = 1;
        while (usedSheetNames.Contains(candidate))
        {
            var suffix = "_" + index++;
            var maxBaseLength = Math.Max(1, 31 - suffix.Length);
            candidate = clean.Length > maxBaseLength ? clean[..maxBaseLength] + suffix : clean + suffix;
        }

        usedSheetNames.Add(candidate);
        return candidate;
    }

    public static string SanitizeSheetName(string? name)
    {
        var value = string.IsNullOrWhiteSpace(name) ? "Sheet" : name.Trim();
        foreach (var c in Path.GetInvalidFileNameChars().Concat(new[] { '[', ']', '*', '?', '/', '\\', ':' }))
        {
            value = value.Replace(c, '_');
        }

        return value.Length > 31 ? value[..31] : value;
    }
}

