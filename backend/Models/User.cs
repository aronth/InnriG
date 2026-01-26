using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace InnriGreifi.API.Models;

public class User : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = true;
    
    [MaxLength(2000)]
    public string? EmailSignature { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public List<WaitTimeNotification> WaitTimeNotifications { get; set; } = new();
    public List<GiftCard> GiftCards { get; set; } = new();
}

