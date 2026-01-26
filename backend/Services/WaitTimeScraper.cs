using HtmlAgilityPack;
using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace InnriGreifi.API.Services;

public class WaitTimeScraper : IWaitTimeScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WaitTimeScraper> _logger;
    private readonly AppDbContext _context;

    public WaitTimeScraper(HttpClient httpClient, ILogger<WaitTimeScraper> logger, AppDbContext context)
    {
        _httpClient = httpClient;
        _logger = logger;
        _context = context;
        
        // Set user agent to avoid being blocked
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<WaitTimeResultDto> ScrapeAsync(Restaurant restaurant)
    {
        if (restaurant.Code == "GRE")
            return await ScrapeGreifinnAsync();
        if (restaurant.Code == "SPR")
            return await ScrapeSpretturinnAsync();
        
        return new WaitTimeResultDto
        {
            Restaurant = restaurant,
            Success = false,
            ErrorMessage = "Unknown restaurant"
        };
    }

    public async Task<WaitTimeResultDto> ScrapeGreifinnAsync()
    {
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Code == "GRE");
        if (restaurant == null)
        {
            return new WaitTimeResultDto
            {
                Restaurant = null!,
                Success = false,
                ErrorMessage = "Greifinn restaurant not found in database"
            };
        }

        try
        {
            var html = await _httpClient.GetStringAsync("https://greifinn.is");
            return ParseGreifinn(html, restaurant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping Greifinn");
            return new WaitTimeResultDto
            {
                Restaurant = restaurant,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<WaitTimeResultDto> ScrapeSpretturinnAsync()
    {
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Code == "SPR");
        if (restaurant == null)
        {
            return new WaitTimeResultDto
            {
                Restaurant = null!,
                Success = false,
                ErrorMessage = "Spretturinn restaurant not found in database"
            };
        }

        try
        {
            var html = await _httpClient.GetStringAsync("https://spretturinn.is");
            return ParseSpretturinn(html, restaurant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping Spretturinn");
            return new WaitTimeResultDto
            {
                Restaurant = restaurant,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private WaitTimeResultDto ParseGreifinn(string html, Restaurant restaurant)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Check if closed
        var closedText = doc.DocumentNode.SelectSingleNode("//*[contains(text(), 'Lokað') or contains(text(), 'lokað')]");
        if (closedText != null)
        {
            return new WaitTimeResultDto
            {
                Restaurant = restaurant,
                IsClosed = true,
                Success = true,
                ScrapedAt = DateTime.UtcNow
            };
        }

        // Try to find wait times
        // Look for "Sótt" and "Sent" or "Heimsent" text
        var bodyText = doc.DocumentNode.InnerText;
        
        int? sottMinutes = null;
        int? sentMinutes = null;

        // Look for "Sótt" followed by time pattern
        var sottMatch = Regex.Match(bodyText, @"Sótt[:\s]*(\d+)\s*(?:mín|min|mínútur|mínútur)", RegexOptions.IgnoreCase);
        if (sottMatch.Success && int.TryParse(sottMatch.Groups[1].Value, out var sott))
        {
            sottMinutes = sott;
        }

        // Look for "Sent" or "Heimsent" followed by time pattern
        var sentMatch = Regex.Match(bodyText, @"(?:Sent|Heimsent)[:\s]*(\d+)\s*(?:mín|min|mínútur|mínútur)", RegexOptions.IgnoreCase);
        if (sentMatch.Success && int.TryParse(sentMatch.Groups[1].Value, out var sent))
        {
            sentMinutes = sent;
        }

        // If no times found, try looking for common patterns in HTML structure
        if (sottMinutes == null && sentMinutes == null)
        {
            // Try to find elements with wait time classes or IDs
            var waitTimeElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'wait') or contains(@class, 'time') or contains(@id, 'wait') or contains(@id, 'time')]");
            if (waitTimeElements != null)
            {
                foreach (var element in waitTimeElements)
                {
                    var text = element.InnerText;
                    var timeMatch = Regex.Match(text, @"(\d+)\s*(?:mín|min|mínútur)", RegexOptions.IgnoreCase);
                    if (timeMatch.Success && int.TryParse(timeMatch.Groups[1].Value, out var minutes))
                    {
                        if (text.Contains("Sótt", StringComparison.OrdinalIgnoreCase))
                        {
                            sottMinutes = minutes;
                        }
                        else if (text.Contains("Sent", StringComparison.OrdinalIgnoreCase) || text.Contains("Heimsent", StringComparison.OrdinalIgnoreCase))
                        {
                            sentMinutes = minutes;
                        }
                    }
                }
            }
        }

        return new WaitTimeResultDto
        {
            Restaurant = restaurant,
            SottMinutes = sottMinutes,
            SentMinutes = sentMinutes,
            IsClosed = false,
            Success = true,
            ScrapedAt = DateTime.UtcNow
        };
    }

    private WaitTimeResultDto ParseSpretturinn(string html, Restaurant restaurant)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Check if closed
        var closedText = doc.DocumentNode.SelectSingleNode("//*[contains(text(), 'Lokað') or contains(text(), 'lokað')]");
        if (closedText != null)
        {
            return new WaitTimeResultDto
            {
                Restaurant = restaurant,
                IsClosed = true,
                Success = true,
                ScrapedAt = DateTime.UtcNow
            };
        }

        // Try to find wait times
        var bodyText = doc.DocumentNode.InnerText;
        
        int? sottMinutes = null;
        int? sentMinutes = null;

        // Look for "Sótt" followed by time pattern
        var sottMatch = Regex.Match(bodyText, @"Sótt[:\s]*(\d+)\s*(?:mín|min|mínútur|mínútur)", RegexOptions.IgnoreCase);
        if (sottMatch.Success && int.TryParse(sottMatch.Groups[1].Value, out var sott))
        {
            sottMinutes = sott;
        }

        // Look for "Sent" or "Heimsent" followed by time pattern
        var sentMatch = Regex.Match(bodyText, @"(?:Sent|Heimsent)[:\s]*(\d+)\s*(?:mín|min|mínútur|mínútur)", RegexOptions.IgnoreCase);
        if (sentMatch.Success && int.TryParse(sentMatch.Groups[1].Value, out var sent))
        {
            sentMinutes = sent;
        }

        // If no times found, try looking for common patterns in HTML structure
        if (sottMinutes == null && sentMinutes == null)
        {
            // Try to find elements with wait time classes or IDs
            var waitTimeElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'wait') or contains(@class, 'time') or contains(@id, 'wait') or contains(@id, 'time')]");
            if (waitTimeElements != null)
            {
                foreach (var element in waitTimeElements)
                {
                    var text = element.InnerText;
                    var timeMatch = Regex.Match(text, @"(\d+)\s*(?:mín|min|mínútur)", RegexOptions.IgnoreCase);
                    if (timeMatch.Success && int.TryParse(timeMatch.Groups[1].Value, out var minutes))
                    {
                        if (text.Contains("Sótt", StringComparison.OrdinalIgnoreCase))
                        {
                            sottMinutes = minutes;
                        }
                        else if (text.Contains("Sent", StringComparison.OrdinalIgnoreCase) || text.Contains("Heimsent", StringComparison.OrdinalIgnoreCase))
                        {
                            sentMinutes = minutes;
                        }
                    }
                }
            }
        }

        return new WaitTimeResultDto
        {
            Restaurant = restaurant,
            SottMinutes = sottMinutes,
            SentMinutes = sentMinutes,
            IsClosed = false,
            Success = true,
            ScrapedAt = DateTime.UtcNow
        };
    }
}
