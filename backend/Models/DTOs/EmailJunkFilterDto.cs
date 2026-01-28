namespace InnriGreifi.API.Models.DTOs;

public class EmailJunkFilterDto
{
    public Guid Id { get; set; }
    public string? Subject { get; set; }
    public string? SenderEmail { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEmailJunkFilterDto
{
    public string? Subject { get; set; }
    public string? SenderEmail { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateEmailJunkFilterDto
{
    public string? Subject { get; set; }
    public string? SenderEmail { get; set; }
    public bool IsActive { get; set; }
}

