namespace InnriGreifi.API.Models.DTOs;

public class UserEmailMappingDto
{
    public Guid Id { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

