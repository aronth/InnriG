namespace InnriGreifi.API.Models.DTOs;

public class OrderImportResultDto
{
    public Guid BatchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }

    public int TotalRowsInSheet { get; set; }
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }

    public List<string> Warnings { get; set; } = new();
}


