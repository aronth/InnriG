namespace InnriGreifi.API.Models.DTOs;

public class EmailConnectionDto
{
    public string DeviceCode { get; set; } = string.Empty;
    public string VerificationUrl { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public int Interval { get; set; }
}


