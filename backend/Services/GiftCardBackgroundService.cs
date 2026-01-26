using InnriGreifi.API.Data;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public interface IGiftCardBackgroundService
{
    string GetBackgroundPath(Guid? restaurantId);
}

public class GiftCardBackgroundService : IGiftCardBackgroundService
{
    private readonly AppDbContext _context;
    
    public GiftCardBackgroundService(AppDbContext context)
    {
        _context = context;
    }
    
    public string GetBackgroundPath(Guid? restaurantId)
    {
        if (!restaurantId.HasValue)
            return "wwwroot/backgrounds/GS_Blank.jpg";
            
        var restaurant = _context.Restaurants.Find(restaurantId.Value);
        return restaurant?.Code switch
        {
            "GRE" => "wwwroot/backgrounds/G_Blank.jpg",
            "SPR" => "wwwroot/backgrounds/S_Blank.jpg",
            _ => "wwwroot/backgrounds/GS_Blank.jpg"
        };
    }
}

