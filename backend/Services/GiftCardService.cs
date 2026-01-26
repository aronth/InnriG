using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public interface IGiftCardService
{
    Task<GiftCard> CreateGiftCardAsync(CreateGiftCardDto dto, Guid? createdById = null);
    Task<List<GiftCard>> CreateGiftCardsBatchAsync(CreateGiftCardBatchDto dto, Guid? createdById = null);
    Task<GiftCard?> GetGiftCardAsync(Guid id);
    Task<List<GiftCard>> GetGiftCardsAsync(GiftCardStatus? status = null, Guid? templateId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<GiftCard> UpdateGiftCardAsync(Guid id, CreateGiftCardDto dto);
    Task<GiftCard> UpdateGiftCardStatusAsync(Guid id, GiftCardStatus status);
}

public class GiftCardService : IGiftCardService
{
    private readonly AppDbContext _context;
    private readonly IGiftCardNumberService _numberService;

    public GiftCardService(AppDbContext context, IGiftCardNumberService numberService)
    {
        _context = context;
        _numberService = numberService;
    }

    public async Task<GiftCard> CreateGiftCardAsync(CreateGiftCardDto dto, Guid? createdById = null)
    {
        var number = await _numberService.GetNextGiftCardNumberAsync();
        
        var giftCard = new GiftCard
        {
            Id = Guid.NewGuid(),
            Number = number,
            TemplateId = dto.TemplateId,
            RestaurantId = dto.RestaurantId,
            Amount = dto.Amount,
            Message = dto.Message,
            ExpirationDate = null, // Will be set when sold
            DkNumber = dto.DkNumber,
            Status = GiftCardStatus.Created,
            CreatedById = createdById,
            PrintWithBackground = dto.PrintWithBackground,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GiftCards.Add(giftCard);
        await _context.SaveChangesAsync();

        return await GetGiftCardAsync(giftCard.Id) ?? giftCard;
    }

    public async Task<List<GiftCard>> CreateGiftCardsBatchAsync(CreateGiftCardBatchDto dto, Guid? createdById = null)
    {
        var giftCards = new List<GiftCard>();
        
        for (int i = 0; i < dto.Count; i++)
        {
            var createDto = new CreateGiftCardDto
            {
                TemplateId = dto.TemplateId,
                RestaurantId = dto.RestaurantId,
                Amount = dto.Amount ?? 0,
                Message = dto.Message,
                PrintWithBackground = dto.PrintWithBackground
            };
            
            var giftCard = await CreateGiftCardAsync(createDto, createdById);
            giftCards.Add(giftCard);
        }

        return giftCards;
    }

    public async Task<GiftCard?> GetGiftCardAsync(Guid id)
    {
        return await _context.GiftCards
            .Include(gc => gc.Template)
            .Include(gc => gc.Restaurant)
            .Include(gc => gc.CreatedBy)
            .FirstOrDefaultAsync(gc => gc.Id == id);
    }

    public async Task<List<GiftCard>> GetGiftCardsAsync(GiftCardStatus? status = null, Guid? templateId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.GiftCards
            .Include(gc => gc.Template)
            .Include(gc => gc.Restaurant)
            .Include(gc => gc.CreatedBy)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(gc => gc.Status == status.Value);
        }

        if (templateId.HasValue)
        {
            query = query.Where(gc => gc.TemplateId == templateId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(gc => gc.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(gc => gc.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(gc => gc.CreatedAt)
            .ToListAsync();
    }

    public async Task<GiftCard> UpdateGiftCardAsync(Guid id, CreateGiftCardDto dto)
    {
        var giftCard = await _context.GiftCards.FindAsync(id);
        
        if (giftCard == null)
        {
            throw new KeyNotFoundException($"Gift card with ID {id} not found");
        }

        giftCard.TemplateId = dto.TemplateId;
        giftCard.RestaurantId = dto.RestaurantId;
        giftCard.Amount = dto.Amount;
        giftCard.Message = dto.Message;
        giftCard.DkNumber = dto.DkNumber;
        giftCard.PrintWithBackground = dto.PrintWithBackground;
        giftCard.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetGiftCardAsync(id) ?? giftCard;
    }

    public async Task<GiftCard> UpdateGiftCardStatusAsync(Guid id, GiftCardStatus status)
    {
        var giftCard = await _context.GiftCards.FindAsync(id);
        
        if (giftCard == null)
        {
            throw new KeyNotFoundException($"Gift card with ID {id} not found");
        }

        giftCard.Status = status;
        giftCard.UpdatedAt = DateTime.UtcNow;

        // Set timestamps and expiration based on status
        if (status == GiftCardStatus.Sold && giftCard.SoldAt == null)
        {
            giftCard.SoldAt = DateTime.UtcNow;
            // Calculate expiration: 2 years from sale date
            giftCard.ExpirationDate = DateTime.UtcNow.AddYears(2);
        }
        else if (status == GiftCardStatus.Redeemed && giftCard.RedeemedAt == null)
        {
            giftCard.RedeemedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await GetGiftCardAsync(id) ?? giftCard;
    }
}



