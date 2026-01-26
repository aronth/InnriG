using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class OrderImportBatch
{
    public Guid Id { get; set; }

    [MaxLength(300)]
    public string FileName { get; set; } = string.Empty;

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public int RowCount { get; set; }

    public List<OrderRow> Rows { get; set; } = new();
}


