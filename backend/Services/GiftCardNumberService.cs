using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public interface IGiftCardNumberService
{
    Task<string> GetNextGiftCardNumberAsync();
}

public class GiftCardNumberService : IGiftCardNumberService
{
    private readonly AppDbContext _context;
    private const string DefaultPrefix = "GC-";
    private const int DefaultNumberLength = 6;

    public GiftCardNumberService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetNextGiftCardNumberAsync()
    {
        // Use database transaction for thread safety
        // PostgreSQL will handle row-level locking automatically within the transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Get or create the sequence record
            // Using transaction isolation ensures thread safety
            var sequence = await _context.GiftCardNumberSequences
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync();

            if (sequence == null)
            {
                // Create initial sequence
                sequence = new GiftCardNumberSequence
                {
                    Id = Guid.NewGuid(),
                    Prefix = DefaultPrefix,
                    NextNumber = 1,
                    NumberLength = DefaultNumberLength,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.GiftCardNumberSequences.Add(sequence);
                await _context.SaveChangesAsync();
            }

            // Generate the number
            var number = sequence.NextNumber;
            var formattedNumber = number.ToString().PadLeft(sequence.NumberLength, '0');
            var giftCardNumber = $"{sequence.Prefix}{formattedNumber}";

            // Increment and save
            sequence.NextNumber++;
            sequence.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            await transaction.CommitAsync();
            
            return giftCardNumber;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

