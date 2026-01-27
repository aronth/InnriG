using ClosedXML.Excel;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimeReportsController : ControllerBase
{
    private static readonly CultureInfo IsCulture = CultureInfo.GetCultureInfo("is-IS");

    /// <summary>
    /// Parse time report Excel file and return employee data with shifts.
    /// </summary>
    [HttpPost("parse")]
    [AllowAnonymous] // Temporarily allow anonymous for testing
    public IActionResult ParseTimeReport([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx files are supported");

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1); // Get first worksheet
            var result = ParseWorksheet(worksheet);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error parsing Excel file: {ex.Message}");
        }
    }

    /// <summary>
    /// Diagnostic endpoint to output raw parsed data as JSON for debugging.
    /// Shows what data is being read from each row.
    /// </summary>
    [HttpPost("parse-debug")]
    [AllowAnonymous] // Temporarily allow anonymous for testing
    public IActionResult ParseDebug([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx files are supported");

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);
            var totalRows = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            int headerRowNumber = FindHeaderRow(worksheet, worksheet.LastColumnUsed()?.ColumnNumber() ?? 0);
            var columnMap = GetColumnMap(worksheet, headerRowNumber);

            var debugData = new
            {
                HeaderRow = headerRowNumber,
                ColumnMap = columnMap,
                Rows = new List<object>()
            };

            // Read first 50 rows for debugging
            for (int row = headerRowNumber + 1; row <= Math.Min(headerRowNumber + 50, totalRows); row++)
            {
                var rowObj = worksheet.Row(row);
                if (rowObj.CellsUsed().Count() == 0)
                    continue;

                var rowData = new Dictionary<string, object?>
                {
                    ["RowNumber"] = row
                };

                // Read all mapped columns
                foreach (var kvp in columnMap)
                {
                    var cell = worksheet.Cell(row, kvp.Value);
                    var value = cell.GetValue<string>()?.Trim();
                    rowData[kvp.Key] = value ?? "(empty)";
                    rowData[$"{kvp.Key}_IsEmpty"] = cell.IsEmpty();
                }

                // Also show the actual KT value we would use
                if (columnMap.ContainsKey("KT"))
                {
                    var ktCol = columnMap["KT"];
                    var ktCell = worksheet.Cell(row, ktCol);
                    var ktValue = ktCell.GetValue<string>()?.Trim();
                    
                    if (string.IsNullOrWhiteSpace(ktValue) && row > headerRowNumber + 1)
                    {
                        // Walk backwards
                        for (int prevRow = row - 1; prevRow > headerRowNumber; prevRow--)
                        {
                            var prevKtCell = worksheet.Cell(prevRow, ktCol);
                            if (!prevKtCell.IsEmpty())
                            {
                                var prevKt = prevKtCell.GetValue<string>()?.Trim();
                                if (!string.IsNullOrWhiteSpace(prevKt))
                                {
                                    ktValue = $"(inherited from row {prevRow}: {prevKt})";
                                    break;
                                }
                            }
                        }
                    }
                    
                    rowData["KT_Resolved"] = ktValue ?? "(empty)";
                }

                debugData.Rows.Add(rowData);
            }

            return Ok(debugData);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error reading Excel file: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Diagnostic endpoint to inspect the structure of an uploaded Excel file.
    /// Returns sheet names, column headers, data types, and sample rows.
    /// </summary>
    [HttpPost("diagnose")]
    [AllowAnonymous] // Temporarily allow anonymous for testing
    public IActionResult DiagnoseExcelFile([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx files are supported");

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var result = new ExcelDiagnosticDto
            {
                FileName = file.FileName,
                FileSizeBytes = file.Length
            };

            foreach (var worksheet in workbook.Worksheets)
            {
                var sheetInfo = AnalyzeWorksheet(worksheet);
                result.Sheets.Add(sheetInfo);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error reading Excel file: {ex.Message}");
        }
    }

    private static SheetInfoDto AnalyzeWorksheet(IXLWorksheet worksheet)
    {
        var sheetInfo = new SheetInfoDto
        {
            Name = worksheet.Name,
            TotalRows = worksheet.LastRowUsed()?.RowNumber() ?? 0,
            TotalColumns = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0
        };

        if (sheetInfo.TotalRows == 0)
            return sheetInfo;

        // Try to find the header row by scanning first 20 rows
        int headerRowNumber = FindHeaderRow(worksheet, sheetInfo.TotalColumns);
        
        // Analyze all columns (not just those with headers in row 1)
        var columns = new List<ColumnInfoDto>();
        
        for (int col = 1; col <= sheetInfo.TotalColumns; col++)
        {
            var header = string.Empty;
            if (headerRowNumber > 0)
            {
                var headerCell = worksheet.Cell(headerRowNumber, col);
                header = headerCell.GetValue<string>()?.Trim() ?? string.Empty;
            }
            
            // If no header found, try to infer from column index
            if (string.IsNullOrEmpty(header))
            {
                header = $"Column{col}";
            }

            var columnInfo = new ColumnInfoDto
            {
                Index = col,
                Header = header,
                NormalizedHeader = NormalizeHeader(header)
            };

            // Analyze data in this column (start from row after header, or row 1 if no header found)
            var startRow = headerRowNumber > 0 ? headerRowNumber + 1 : 1;
            AnalyzeColumn(worksheet, col, columnInfo, sheetInfo.TotalRows, startRow);
            columns.Add(columnInfo);
        }

        sheetInfo.Columns = columns.OrderBy(c => c.Index).ToList();
        sheetInfo.DataRows = headerRowNumber > 0 
            ? Math.Max(0, sheetInfo.TotalRows - headerRowNumber) 
            : sheetInfo.TotalRows;

        // Get sample rows (first 20 data rows)
        var sampleStartRow = headerRowNumber > 0 ? headerRowNumber + 1 : 1;
        sheetInfo.SampleRows = GetSampleRows(worksheet, columns, Math.Min(20, sheetInfo.DataRows), sampleStartRow);

        return sheetInfo;
    }

    private static int FindHeaderRow(IXLWorksheet worksheet, int totalColumns)
    {
        // Scan first 20 rows to find the header row
        // A header row typically has multiple non-empty cells with text values
        // Look for common header patterns like "Nafn", "KT", "Frá", "Til"
        for (int row = 1; row <= Math.Min(20, worksheet.LastRowUsed()?.RowNumber() ?? 0); row++)
        {
            var rowObj = worksheet.Row(row);
            var nonEmptyCount = 0;
            var textCount = 0;
            var headerKeywords = 0;
            
            for (int col = 1; col <= totalColumns; col++)
            {
                var cell = rowObj.Cell(col);
                if (!cell.IsEmpty())
                {
                    nonEmptyCount++;
                    var value = cell.GetValue<string>()?.Trim();
                    // Check if it looks like a header (short text, not a date/number pattern)
                    if (!string.IsNullOrWhiteSpace(value) && value.Length < 50)
                    {
                        textCount++;
                        // Check for known header keywords
                        var normalized = value.ToLowerInvariant();
                        if (normalized.Contains("nafn") || normalized.Contains("kt") || 
                            normalized.Contains("fra") || normalized.Contains("til") ||
                            normalized.Contains("verk") || normalized.Contains("tegund"))
                        {
                            headerKeywords++;
                        }
                    }
                }
            }
            
            // If this row has multiple text cells and header keywords, it's likely the header row
            if (nonEmptyCount >= 3 && textCount >= 3 && headerKeywords >= 2)
            {
                return row;
            }
        }
        
        return 0; // No header row found
    }

    private static void AnalyzeColumn(IXLWorksheet worksheet, int columnIndex, ColumnInfoDto columnInfo, int totalRows, int startRow = 1)
    {
        var sampleValues = new List<object?>();
        var nonEmptyCount = 0;
        var typeCounts = new Dictionary<string, int>();

        // Analyze up to 100 rows for performance
        var rowsToAnalyze = Math.Min(100, totalRows - startRow + 1);
        for (int row = startRow; row < startRow + rowsToAnalyze && row <= totalRows; row++)
        {
            var cell = worksheet.Cell(row, columnIndex);
            if (cell.IsEmpty())
                continue;

            nonEmptyCount++;
            var value = GetCellValue(cell);

            if (value != null)
            {
                sampleValues.Add(value);
                var type = value.GetType().Name;
                typeCounts[type] = typeCounts.GetValueOrDefault(type, 0) + 1;
            }
        }

        columnInfo.NonEmptyCount = nonEmptyCount;
        columnInfo.SampleValue = sampleValues.FirstOrDefault();

        // Determine most common type
        if (typeCounts.Any())
        {
            var mostCommonType = typeCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            columnInfo.DetectedType = mostCommonType;
        }
        else
        {
            columnInfo.DetectedType = "Empty";
        }
    }

    private static object? GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty())
            return null;

        // Try different types
        if (cell.DataType == XLDataType.DateTime)
        {
            if (cell.TryGetValue<DateTime>(out var dt))
                return dt;
        }
        else if (cell.DataType == XLDataType.Number)
        {
            if (cell.TryGetValue<decimal>(out var dec))
                return dec;
            if (cell.TryGetValue<double>(out var dbl))
                return dbl;
        }
        else if (cell.DataType == XLDataType.Boolean)
        {
            if (cell.TryGetValue<bool>(out var b))
                return b;
        }

        // Default to string
        return cell.GetValue<string>()?.Trim();
    }

    private static List<Dictionary<string, object?>> GetSampleRows(IXLWorksheet worksheet, List<ColumnInfoDto> columns, int count, int startRow = 1)
    {
        var sampleRows = new List<Dictionary<string, object?>>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (int row = startRow; row < startRow + count && row <= lastRow; row++)
        {
            var rowData = new Dictionary<string, object?>();
            var rowObj = worksheet.Row(row);

            foreach (var col in columns)
            {
                var cell = rowObj.Cell(col.Index);
                var value = GetCellValue(cell);
                rowData[col.Header] = value;
            }

            // Only add non-empty rows
            if (rowData.Values.Any(v => v != null))
            {
                sampleRows.Add(rowData);
            }
        }

        return sampleRows;
    }

    private static string NormalizeHeader(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var s = input.Trim().ToLowerInvariant();

        // Remove diacritics
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(ch);
        }

        var ascii = sb.ToString()
            .Replace('\u00A0', ' ')
            .Replace("/", " ")
            .Replace(".", " ");

        // Collapse whitespace
        ascii = string.Join(" ", ascii.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return ascii;
    }

    private static TimeReportParseResultDto ParseWorksheet(IXLWorksheet worksheet)
    {
        var result = new TimeReportParseResultDto();
        var employees = new Dictionary<string, EmployeeTimeReportDto>();

        var totalRows = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        if (totalRows == 0)
            return result;

        // Find header row
        int headerRowNumber = FindHeaderRow(worksheet, worksheet.LastColumnUsed()?.ColumnNumber() ?? 0);
        if (headerRowNumber == 0)
            return result;

        // Get column indices by header
        var columnMap = GetColumnMap(worksheet, headerRowNumber);

        // Parse data rows
        var startDate = (DateTime?)null;
        var endDate = (DateTime?)null;

        for (int row = headerRowNumber + 1; row <= totalRows; row++)
        {
            // Skip completely empty rows (these separate employees)
            var rowObj = worksheet.Row(row);
            if (rowObj.CellsUsed().Count() == 0)
                continue;

            // Get employee identifier (KT - Kennitala)
            if (!columnMap.ContainsKey("KT"))
                continue;
                
            // Use worksheet.Cell() to ensure we're reading from the correct row
            var ktCol = columnMap["KT"];
            var ktCell = worksheet.Cell(row, ktCol);
            
            // Get Kennitala - read the actual cell value
            var kennitala = GetCellValueAsString(ktCell);
            
            // If KT is empty, this might be an empty separator row between employees
            // Skip it - don't try to inherit from previous row
            if (string.IsNullOrWhiteSpace(kennitala))
                continue;
            
            // Ensure consistent trimming for dictionary key
            kennitala = kennitala.Trim();

            // Get or create employee
            if (!employees.TryGetValue(kennitala, out var employee))
            {
                var name = GetCellValueSafely(worksheet, row, columnMap, "Nafn");
                employee = new EmployeeTimeReportDto
                {
                    Kennitala = kennitala,
                    Name = name ?? string.Empty
                };
                employees[kennitala] = employee;
            }
            else
            {
                // Update name if it's empty and we have a new value
                var name = GetCellValueSafely(worksheet, row, columnMap, "Nafn");
                if (string.IsNullOrWhiteSpace(employee.Name) && !string.IsNullOrWhiteSpace(name))
                {
                    employee.Name = name;
                }
            }

            // Parse shift data
            var shift = ParseShift(worksheet, row, columnMap);
            if (shift != null)
            {
                employee.Shifts.Add(shift);

                // Track date range
                if (startDate == null || shift.StartTime < startDate)
                    startDate = shift.StartTime;
                if (endDate == null || shift.EndTime > endDate)
                    endDate = shift.EndTime;
            }
        }

        // Calculate total hours for each employee
        foreach (var employee in employees.Values)
        {
            var totalHours = TimeSpan.Zero;
            foreach (var shift in employee.Shifts)
            {
                totalHours = totalHours.Add(shift.Duration);
            }
            employee.TotalHours = totalHours;
        }

        result.Employees = employees.Values.OrderBy(e => e.Name).ToList();
        result.TotalShifts = result.Employees.Sum(e => e.Shifts.Count);
        result.ReportStartDate = startDate;
        result.ReportEndDate = endDate;

        return result;
    }

    private static Dictionary<string, int> GetColumnMap(IXLWorksheet worksheet, int headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rowObj = worksheet.Row(headerRow);
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int col = 1; col <= lastCol; col++)
        {
            var header = GetCellValueAsString(rowObj.Cell(col));
            if (!string.IsNullOrWhiteSpace(header))
            {
                var normalized = NormalizeHeader(header);
                // Map common variations - use if/else to prevent multiple mappings
                if (normalized.Contains("nafn") && !map.ContainsKey("Nafn"))
                    map["Nafn"] = col;
                else if (normalized == "kt" || (normalized.Contains("kt") && !normalized.Contains("hopanumer") && !map.ContainsKey("KT")))
                    map["KT"] = col;
                else if ((normalized.Contains("hopanumer") || normalized.Contains("hopanumer")) && !map.ContainsKey("Hópanúmer"))
                    map["Hópanúmer"] = col;
                else if (normalized.Contains("hopur") && !map.ContainsKey("Hópur"))
                    map["Hópur"] = col;
                else if (normalized.Contains("verk") && !map.ContainsKey("Verk"))
                    map["Verk"] = col;
                else if ((normalized.Contains("fra") || normalized.Contains("from")) && !map.ContainsKey("Frá"))
                    map["Frá"] = col;
                else if ((normalized.Contains("til") || normalized.Contains("to")) && !map.ContainsKey("Til"))
                    map["Til"] = col;
                else if ((normalized.Contains("tegund") || normalized.Contains("type")) && !map.ContainsKey("Tegund"))
                    map["Tegund"] = col;
                else if ((normalized.Contains("samykkt") || normalized.Contains("approved")) && !map.ContainsKey("Samþykkt"))
                    map["Samþykkt"] = col;
                else if (normalized.Contains("ath") && normalized.Contains("stimplun") && !map.ContainsKey("Ath á stimplun"))
                    map["Ath á stimplun"] = col;
                else if (normalized.Contains("ath") && normalized.Contains("dags") && !map.ContainsKey("Ath dags"))
                    map["Ath dags"] = col;
            }
        }

        return map;
    }

    private static ShiftDto? ParseShift(IXLWorksheet worksheet, int row, Dictionary<string, int> columnMap)
    {
        // Get required fields
        var fromStr = GetCellValueSafely(worksheet, row, columnMap, "Frá");
        var toStr = GetCellValueSafely(worksheet, row, columnMap, "Til");

        if (string.IsNullOrWhiteSpace(fromStr) || string.IsNullOrWhiteSpace(toStr))
            return null;

        // Parse Icelandic date format: "dd.MM.yyyy HH:mm"
        if (!TryParseIcelandicDateTime(fromStr, out var startTime) ||
            !TryParseIcelandicDateTime(toStr, out var endTime))
            return null;

        var shift = new ShiftDto
        {
            StartTime = startTime,
            EndTime = endTime
        };

        // Get optional fields
        shift.WorkLocation = GetCellValueSafely(worksheet, row, columnMap, "Verk");
        shift.Type = GetCellValueSafely(worksheet, row, columnMap, "Tegund");
        shift.Approved = GetCellValueSafely(worksheet, row, columnMap, "Samþykkt");
        shift.ClockInNote = GetCellValueSafely(worksheet, row, columnMap, "Ath á stimplun");
        shift.DayNote = GetCellValueSafely(worksheet, row, columnMap, "Ath dags");
        shift.GroupNumber = GetCellValueSafely(worksheet, row, columnMap, "Hópanúmer");
        shift.Group = GetCellValueSafely(worksheet, row, columnMap, "Hópur");

        return shift;
    }

    private static bool TryParseIcelandicDateTime(string? value, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Try parsing with Icelandic format: "dd.MM.yyyy HH:mm"
        var formats = new[]
        {
            "dd.MM.yyyy HH:mm",
            "dd.MM.yyyy H:mm",
            "dd.MM.yyyy",
            "d.M.yyyy HH:mm",
            "d.M.yyyy H:mm",
            "d.M.yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value.Trim(), format, IsCulture, DateTimeStyles.None, out result))
            {
                // If no time component, assume start of day
                if (!format.Contains("HH") && !format.Contains("H"))
                {
                    result = result.Date;
                }
                return true;
            }
        }

        // Fallback to standard parsing
        return DateTime.TryParse(value, IsCulture, DateTimeStyles.None, out result);
    }

    private static string? GetCellValueAsString(IXLCell cell)
    {
        if (cell == null)
            return null;
            
        // Check if cell is actually empty (not merged with a value)
        if (cell.IsEmpty())
            return null;

        // Get the actual value from this cell
        var value = cell.GetValue<string>();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? GetCellValueSafely(IXLWorksheet worksheet, int row, Dictionary<string, int> columnMap, string columnKey)
    {
        if (!columnMap.TryGetValue(columnKey, out var colIndex) || colIndex <= 0)
            return null;

        return GetCellValueAsString(worksheet.Cell(row, colIndex));
    }
}

