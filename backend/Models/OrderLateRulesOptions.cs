namespace InnriGreifi.API.Models;

public class OrderLateRulesOptions
{
    public const string SectionName = "OrderLateRules";
    
    public int SentThresholdMinutes { get; set; } = 15;
    public int OtherThresholdMinutes { get; set; } = 7;
}

