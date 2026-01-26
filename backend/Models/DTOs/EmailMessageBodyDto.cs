namespace InnriGreifi.API.Models.DTOs;

public class EmailMessageBodyDto
{
    public string? Html { get; set; }
    public string? Text { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

