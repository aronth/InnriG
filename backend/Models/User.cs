using Microsoft.AspNetCore.Identity;

namespace InnriGreifi.API.Models;

public class User : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public List<WaitTimeNotification> WaitTimeNotifications { get; set; } = new();
}

