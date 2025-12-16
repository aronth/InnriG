namespace InnriGreifi.API.Models.DTOs;

public class UnifiedInventoryListDto
{
    public string Birgir { get; set; } = string.Empty; // Supplier name
    public string Vorunumer { get; set; } = string.Empty; // Product code
    public string Voruheiti { get; set; } = string.Empty; // Product name
    public string Eining { get; set; } = string.Empty; // Unit (kg, L, stk)
    public decimal VerdAnVsk { get; set; } // Unit price without VAT
    public decimal SkilagjoldUmbudagjold { get; set; } = 0; // Return/packaging fees (not available in invoice data)
    public decimal NettoInnkaupsverdPerEiningu { get; set; } // Calculated: VerdAnVsk + SkilagjoldUmbudagjold
    public DateTime DagsetningSidustuUppfaerslu { get; set; } // Last update date
    public string? SidastiReikningsnumer { get; set; } // Last invoice number (for traceability)
}

