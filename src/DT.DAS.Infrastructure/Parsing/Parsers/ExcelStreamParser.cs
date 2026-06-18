using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using NPOI.SS.UserModel;

namespace DT.DAS.Infrastructure.Parsing.Parsers;

public sealed class ExcelStreamParser : BaseStreamParser, IDataParser
{
    public List<T> Parse<T>(Stream stream, object? options = null) where T : class, new()
    {
        var opt = options as ExcelParserOptions ?? new ExcelParserOptions();
        var result = new List<T>();
        using var workbook = WorkbookFactory.Create(stream);
        var sheet = string.IsNullOrWhiteSpace(opt.SheetName) ? workbook.GetSheetAt(0) : workbook.GetSheet(opt.SheetName);
        if (sheet == null)
        {
            return result;
        }

        var headerRow = sheet.GetRow(opt.HeaderRow - 1);
        if (headerRow == null)
        {
            return result;
        }

        var headers = Enumerable.Range(0, headerRow.LastCellNum)
            .Select(i => (headerRow.GetCell(i)?.ToString() ?? string.Empty).Trim())
            .ToArray();

        for (var rowIndex = opt.StartRow - 1; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null || (opt.SkipEmptyLines && IsEmptyRow(row)))
            {
                continue;
            }

            var values = Enumerable.Range(0, headers.Length)
                .Select(i => GetCellValue(row.GetCell(i, MissingCellPolicy.CREATE_NULL_AS_BLANK)))
                .ToArray();

            result.Add(MapToEntity<T>(headers, values, rowIndex + 1, opt.HasExtFields, opt.FilePath));
        }

        return result;
    }

    public Task<List<T>> ParseAsync<T>(Stream stream, object? options = null, CancellationToken ct = default) where T : class, new()
    {
        return Task.Run(() => Parse<T>(stream, options), ct);
    }

    private static bool IsEmptyRow(IRow row)
    {
        for (var i = 0; i < row.LastCellNum; i++)
        {
            var cell = row.GetCell(i);
            if (cell != null && cell.CellType != CellType.Blank && !string.IsNullOrWhiteSpace(cell.ToString()))
            {
                return false;
            }
        }

        return true;
    }

    private static object? GetCellValue(ICell? cell)
    {
        if (cell == null)
        {
            return null;
        }

        return cell.CellType switch
        {
            CellType.Numeric when DateUtil.IsCellDateFormatted(cell) => cell.DateCellValue,
            CellType.Numeric => cell.NumericCellValue,
            CellType.Boolean => cell.BooleanCellValue,
            CellType.String => cell.StringCellValue,
            CellType.Formula => cell.ToString(),
            _ => cell.ToString()
        };
    }
}

