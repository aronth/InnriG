namespace InnriGreifi.API.Models.DTOs;

public class EmailConnectionStatusDto
{
    public string EmailAddress { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsSystemInbox { get; set; }
    public DateTime? LastRefreshedAt { get; set; }
}


