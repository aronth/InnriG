using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class OrderRow
{
    public Guid Id { get; set; }

    public Guid OrderImportBatchId { get; set; }

    [JsonIgnore]
    public OrderImportBatch OrderImportBatch { get; set; } = null!;

    public Guid? RestaurantId { get; set; }

    [JsonIgnore]
    public Restaurant? Restaurant { get; set; }

    public int SourceRowNumber { get; set; }

    [MaxLength(100)]
    public string? Status { get; set; } // Stada

    public DateTime? Date { get; set; } // Dagsetning

    [MaxLength(200)]
    public string? Salesman { get; set; } // Sölumadur

    [MaxLength(200)]
    public string? Debtor { get; set; } // Skuldunautur

    public decimal? TotalAmountWithVat { get; set; } // Samtals upph. m/VSK

    public int? CashRegisterNumber { get; set; } // Kassi nr.

    public int? InvoiceNumber { get; set; } // Reikningur nr.

    [MaxLength(500)]
    public string? Description { get; set; } // Lysing

    public bool? CreatedOnRegister { get; set; } // Stofnad á kassa

    [MaxLength(200)]
    public string? OrderType { get; set; } // Tegund pöntunar

    public DateTime? CreatedDate { get; set; } // Stofndagur

    [MaxLength(500)]
    public string? Address { get; set; } // Heimilisfang

    public DateTime? OrderDate { get; set; } // Pöntunardags.

    public DateTime? DeliveryDate { get; set; } // Afhendingardags.

    public bool? Deleted { get; set; } // Eytt

    public string? InvoiceText3Raw { get; set; } // Reikningstexti 3 (raw)

    public DateTime? ScannedAt { get; set; } // parsed from InvoiceText3Raw

    public DateTime? CheckedOutAt { get; set; } // parsed from InvoiceText3Raw

    [MaxLength(100)]
    public string? OrderNumber { get; set; } // Pöntunarnúmer

    // Derived / computed fields (stored for reporting)
    [MaxLength(20)]
    public string DeliveryMethod { get; set; } = "Unknown"; // Sótt / Sent / Salur / Unknown

    [MaxLength(20)]
    public string OrderSource { get; set; } = "Unknown"; // Counter / Web / Unknown

    public TimeOnly? OrderTime { get; set; } // from Stofndagur (time portion)

    public TimeOnly? ReadyTime { get; set; } // from Afhendingardags. (time portion)

    public int? WaitTimeMin { get; set; } // ReadyTime - OrderTime, rounded to 5 min

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


