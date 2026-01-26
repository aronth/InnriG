using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Manager,Admin")]
public class GiftCardsController : ControllerBase
{
    private readonly IGiftCardService _giftCardService;
    private readonly IGiftCardPdfService _pdfService;
    private readonly AppDbContext _context;

    public GiftCardsController(IGiftCardService giftCardService, IGiftCardPdfService pdfService, AppDbContext context)
    {
        _giftCardService = giftCardService;
        _pdfService = pdfService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetGiftCards(
        [FromQuery] string? status = null,
        [FromQuery] Guid? templateId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        GiftCardStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<GiftCardStatus>(status, true, out var parsedStatus))
        {
            statusEnum = parsedStatus;
        }

        var giftCards = await _giftCardService.GetGiftCardsAsync(statusEnum, templateId, fromDate, toDate);
        
        var dtos = giftCards.Select(gc => MapToDto(gc)).ToList();
        
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGiftCard(Guid id)
    {
        var giftCard = await _giftCardService.GetGiftCardAsync(id);
        
        if (giftCard == null)
            return NotFound();

        return Ok(MapToDto(giftCard));
    }

    [HttpPost]
    public async Task<IActionResult> CreateGiftCard([FromBody] CreateGiftCardDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var giftCard = await _giftCardService.CreateGiftCardAsync(dto, userId);
        
        return CreatedAtAction(nameof(GetGiftCard), new { id = giftCard.Id }, MapToDto(giftCard));
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateGiftCardsBatch([FromBody] CreateGiftCardBatchDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (dto.Count < 1 || dto.Count > 100)
            return BadRequest("Count must be between 1 and 100");

        var userId = GetCurrentUserId();
        var giftCards = await _giftCardService.CreateGiftCardsBatchAsync(dto, userId);
        
        var dtos = giftCards.Select(gc => MapToDto(gc)).ToList();
        
        return CreatedAtAction(nameof(GetGiftCards), dtos);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGiftCard(Guid id, [FromBody] CreateGiftCardDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var giftCard = await _giftCardService.UpdateGiftCardAsync(id, dto);
            return Ok(MapToDto(giftCard));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateGiftCardStatus(Guid id, [FromBody] UpdateGiftCardStatusDto dto)
    {
        if (!Enum.TryParse<GiftCardStatus>(dto.Status, true, out var status))
            return BadRequest($"Invalid status: {dto.Status}");

        try
        {
            var giftCard = await _giftCardService.UpdateGiftCardStatusAsync(id, status);
            return Ok(MapToDto(giftCard));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("preview")]
    public async Task<IActionResult> PreviewGiftCard([FromBody] GiftCardPreviewDto dto)
    {
        // Create temporary GiftCard for preview (not saved)
        var previewCard = new GiftCard
        {
            Id = Guid.NewGuid(),
            Number = "PREVIEW-001",
            Amount = dto.Amount,
            Message = dto.Message,
            RestaurantId = dto.RestaurantId,
            Restaurant = dto.RestaurantId.HasValue 
                ? await _context.Restaurants.FindAsync(dto.RestaurantId.Value)
                : null,
            ExpirationDate = null,  // Always null for preview
            DkNumber = dto.DkNumber,
            PrintWithBackground = dto.PrintWithBackground
        };
        
        var pdfBytes = _pdfService.GeneratePdf(previewCard, dto.PrintWithBackground);
        return File(pdfBytes, "application/pdf", "gjafakort_preview.pdf");
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GeneratePdf(Guid id, [FromQuery] bool includeBackground = false)
    {
        var giftCard = await _giftCardService.GetGiftCardAsync(id);
        
        if (giftCard == null)
            return NotFound();

        var pdfBytes = _pdfService.GeneratePdf(giftCard, includeBackground);
        
        var fileName = $"gjafakort_{giftCard.Number}_{DateTime.UtcNow:yyyyMMdd}.pdf";
        
        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGiftCard(Guid id)
    {
        var giftCard = await _giftCardService.GetGiftCardAsync(id);
        
        if (giftCard == null)
            return NotFound();

        // For now, hard delete. Can be changed to soft delete later if needed
        // This would require adding IsDeleted flag to GiftCard model
        return BadRequest("Delete functionality not implemented. Consider updating status to Expired instead.");
    }

    private GiftCardDto MapToDto(GiftCard gc)
    {
        return new GiftCardDto
        {
            Id = gc.Id,
            Number = gc.Number,
            TemplateId = gc.TemplateId,
            TemplateName = gc.Template?.Name,
            RestaurantId = gc.RestaurantId,
            RestaurantName = gc.Restaurant?.Name,
            RestaurantCode = gc.Restaurant?.Code,
            Amount = gc.Amount,
            Message = gc.Message,
            ExpirationDate = gc.ExpirationDate,
            DkNumber = gc.DkNumber,
            Status = gc.Status.ToString(),
            CreatedById = gc.CreatedById,
            CreatedByName = gc.CreatedBy?.Name,
            SoldAt = gc.SoldAt,
            RedeemedAt = gc.RedeemedAt,
            PrintWithBackground = gc.PrintWithBackground,
            CreatedAt = gc.CreatedAt,
            UpdatedAt = gc.UpdatedAt
        };
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }
}



