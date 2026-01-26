namespace InnriGreifi.API.Models;

public class GiftCardNumberSequence
{
    public Guid Id { get; set; }
    public string Prefix { get; set; } = "GC-";
    public int NextNumber { get; set; } = 1;
    public int NumberLength { get; set; } = 6;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}



