namespace InnriGreifi.API.Models.DTOs;

/// <summary>
/// Diagnostic information about an Excel file structure.
/// </summary>
public class ExcelDiagnosticDto
{
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public List<SheetInfoDto> Sheets { get; set; } = new();
}

/// <summary>
/// Information about a single worksheet in the Excel file.
/// </summary>
public class SheetInfoDto
{
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public int DataRows { get; set; }
    public List<ColumnInfoDto> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> SampleRows { get; set; } = new();
}

/// <summary>
/// Information about a column in the worksheet.
/// </summary>
public class ColumnInfoDto
{
    public int Index { get; set; }
    public string Header { get; set; } = string.Empty;
    public string? NormalizedHeader { get; set; }
    public string DetectedType { get; set; } = string.Empty;
    public int NonEmptyCount { get; set; }
    public object? SampleValue { get; set; }
}

