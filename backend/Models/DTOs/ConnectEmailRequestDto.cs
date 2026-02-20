namespace InnriGreifi.API.Models.DTOs;

public class ConnectEmailRequestDto
{
    public string EmailAddress { get; set; } = string.Empty;
    public bool IsSystemInbox { get; set; }
}


