namespace InnriGreifi.API.Models.DTOs;

public class OrderLateCountByMethodDto
{
    public string DeliveryMethod { get; set; } = "Unknown";
    public int Total { get; set; }
    public int Evaluable { get; set; }
    public int LateCount { get; set; }
    public decimal LateRatio => Evaluable == 0 ? 0 : (decimal)LateCount / Evaluable;
}


