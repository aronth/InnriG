using ClosedXML.Excel;
using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;

namespace InnriGreifi.API.Services;

public class OrderImportService : IOrderImportService
{
    private static readonly CultureInfo IsCulture = CultureInfo.GetCultureInfo("is-IS");

    private readonly AppDbContext _context;
    private readonly ILogger<OrderImportService> _logger;

    public OrderImportService(AppDbContext context, ILogger<OrderImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrderImportResultDto> ImportAsync(IFormFile file, Guid? restaurantId, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty", nameof(file));

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only .xlsx files are supported", nameof(file));

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        // Sheet name can vary; per requirement there is always exactly one sheet.
        var worksheet = workbook.Worksheets.First();

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        // Exclude header row (1) and totals row (last) => data rows count
        var lastDataRow = Math.Max(1, lastRowUsed - 1);
        var totalRowsInSheet = Math.Max(0, lastDataRow - 1);

        var batch = new OrderImportBatch
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            ImportedAt = DateTime.UtcNow
        };

        // Save batch first so we have the ID for foreign key relationships
        _context.OrderImportBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        var imported = 0;
        var skipped = 0;
        var warnings = new List<string>();

        // Column mapping: support rearranged columns by using header row values.
        var headerMap = BuildHeaderMap(worksheet, warnings);

        _logger.LogInformation("Starting import of {FileName}. Total rows to process: {TotalRows}", 
            file.FileName, totalRowsInSheet);

        // Disable change tracking for better performance during bulk inserts
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        const int batchSize = 1000; // Save in chunks of 1000 rows
        var rowsToSave = new List<OrderRow>(batchSize);
        var lastLogTime = DateTime.UtcNow;
        const int logIntervalSeconds = 10; // Log progress every 10 seconds

        try
        {
            for (var r = 2; r <= lastDataRow; r++)
            {
                var row = worksheet.Row(r);
                if (IsRowEmpty(row, 1, 17))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var entity = new OrderRow
                    {
                        Id = Guid.NewGuid(),
                        OrderImportBatchId = batch.Id,
                        RestaurantId = restaurantId,
                        SourceRowNumber = r,
                        Status = GetString(TryGetCell(row, headerMap, "status")),
                        Date = GetDateTime(TryGetCell(row, headerMap, "date")),
                        Salesman = GetString(TryGetCell(row, headerMap, "salesman")),
                        Debtor = GetString(TryGetCell(row, headerMap, "debtor")),
                        TotalAmountWithVat = GetDecimal(TryGetCell(row, headerMap, "totalAmountWithVat")),
                        CashRegisterNumber = GetInt(TryGetCell(row, headerMap, "cashRegisterNumber")),
                        InvoiceNumber = GetInt(TryGetCell(row, headerMap, "invoiceNumber")),
                        Description = GetString(TryGetCell(row, headerMap, "description")),
                        CreatedOnRegister = GetBool(TryGetCell(row, headerMap, "createdOnRegister")),
                        OrderType = GetString(TryGetCell(row, headerMap, "orderType")),
                        CreatedDate = GetDateTime(TryGetCell(row, headerMap, "createdDate")),
                        Address = GetString(TryGetCell(row, headerMap, "address")),
                        OrderDate = GetDateTime(TryGetCell(row, headerMap, "orderDate")),
                        DeliveryDate = GetDateTime(TryGetCell(row, headerMap, "deliveryDate")),
                        Deleted = GetBool(TryGetCell(row, headerMap, "deleted")),
                        InvoiceText3Raw = GetString(TryGetCell(row, headerMap, "invoiceText3Raw")),
                        OrderNumber = GetString(TryGetCell(row, headerMap, "orderNumber")),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // If deleted ("Eytt" == Já) skip import
                    if (entity.Deleted == true)
                    {
                        skipped++;
                        continue;
                    }

                    var (scannedAtUtc, checkedOutAtUtc) = OrderTextParser.ParseReikningstexti3(entity.InvoiceText3Raw);
                    entity.ScannedAt = scannedAtUtc;
                    entity.CheckedOutAt = checkedOutAtUtc;

                    // Derived fields
                    entity.DeliveryMethod = OrderDerivedFields.ComputeDeliveryMethod(entity.OrderType, entity.InvoiceText3Raw, entity.OrderNumber);
                    entity.OrderSource = OrderDerivedFields.ComputeOrderSource(entity.OrderNumber);
                    var (orderTime, readyTime, waitTimeMin) = OrderDerivedFields.ComputeTimes(entity.CreatedDate, entity.DeliveryDate);
                    entity.OrderTime = orderTime;
                    entity.ReadyTime = readyTime;
                    entity.WaitTimeMin = waitTimeMin;

                    rowsToSave.Add(entity);
                    imported++;

                    // Log progress periodically to help diagnose timeout issues
                    var now = DateTime.UtcNow;
                    if ((now - lastLogTime).TotalSeconds >= logIntervalSeconds)
                    {
                        var progress = (double)(r - 1) / totalRowsInSheet * 100;
                        _logger.LogInformation("Import progress: {CurrentRow}/{TotalRows} ({Progress:F1}%) - Imported: {Imported}, Skipped: {Skipped}", 
                            r - 1, totalRowsInSheet, progress, imported, skipped);
                        lastLogTime = now;
                    }

                    // Save in batches to avoid memory issues and improve performance
                    if (rowsToSave.Count >= batchSize)
                    {
                        _context.OrderRows.AddRange(rowsToSave);
                        await _context.SaveChangesAsync(cancellationToken);
                        _context.ChangeTracker.Clear(); // Clear tracking to free memory
                        rowsToSave.Clear();
                        
                        _logger.LogDebug("Saved batch of {BatchSize} rows. Total imported so far: {Imported}", 
                            batchSize, imported);
                    }
                }
                catch (Exception ex)
                {
                    skipped++;
                    warnings.Add($"Row {r}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to import order row {RowNumber} from file {FileName}", r, file.FileName);
                }
            }

            // Save remaining rows
            if (rowsToSave.Count > 0)
            {
                _context.OrderRows.AddRange(rowsToSave);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Update batch row count
            batch.RowCount = imported;
            _context.OrderImportBatches.Update(batch);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Import completed for {FileName}. Imported: {Imported}, Skipped: {Skipped}, Warnings: {WarningCount}", 
                file.FileName, imported, skipped, warnings.Count);
        }
        finally
        {
            // Re-enable change tracking
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        return new OrderImportResultDto
        {
            BatchId = batch.Id,
            FileName = batch.FileName,
            ImportedAt = batch.ImportedAt,
            TotalRowsInSheet = totalRowsInSheet,
            ImportedRows = imported,
            SkippedRows = skipped,
            Warnings = warnings
        };
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet, List<string> warnings)
    {
        // Canonical key -> acceptable header names (normalized)
        var synonyms = new Dictionary<string, string[]>
        {
            ["status"] = new[] { "stada", "staða" },
            ["date"] = new[] { "dagsetning" },
            ["salesman"] = new[] { "solumadur", "sölumadur" },
            ["debtor"] = new[] { "skuldunautur" },
            ["totalAmountWithVat"] = new[] { "samtals upph mvsk", "samtals upph. mvsk", "samtals upph m/vsk", "samtals upph. m/vsk" },
            ["cashRegisterNumber"] = new[] { "kassi nr", "kassi nr." },
            ["invoiceNumber"] = new[] { "reikningur nr", "reikningur nr." },
            ["description"] = new[] { "lysing", "lýsing" },
            ["createdOnRegister"] = new[] { "stofnad a kassa", "stofnad á kassa" },
            ["orderType"] = new[] { "tegund pontunar", "tegund pöntunar" },
            ["createdDate"] = new[] { "stofndagur" },
            ["address"] = new[] { "heimilisfang" },
            ["orderDate"] = new[] { "pontunardags", "pontunardags.", "pöntunardags", "pöntunardags." },
            ["deliveryDate"] = new[] { "afhendingardags", "afhendingardags.", "afhendingardags.", "afhendingardags" },
            ["deleted"] = new[] { "eytt" },
            ["invoiceText3Raw"] = new[] { "reikningstexti 3" },
            ["orderNumber"] = new[] { "pontunarnumer", "pöntunarnúmer" }
        };

        // Build reverse index from normalized header string -> column number
        var headerRow = worksheet.Row(1);
        var used = headerRow.CellsUsed().ToList();
        var headerToCol = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in used)
        {
            var raw = c.GetValue<string>();
            var norm = NormalizeHeader(raw);
            if (string.IsNullOrWhiteSpace(norm))
                continue;

            headerToCol[norm] = c.Address.ColumnNumber;
        }

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, names) in synonyms)
        {
            int? col = null;
            foreach (var n in names.Select(NormalizeHeader))
            {
                if (headerToCol.TryGetValue(n, out var cn))
                {
                    col = cn;
                    break;
                }
            }

            if (col != null)
                map[key] = col.Value;
            else
                warnings.Add($"Missing column: {names[0]}");
        }

        return map;
    }

    private static IXLCell? TryGetCell(IXLRow row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col) || col <= 0)
            return null;

        return row.Cell(col);
    }

    private static string NormalizeHeader(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var s = input.Trim().ToLowerInvariant();

        // remove diacritics
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(ch);
        }

        var ascii = sb.ToString()
            .Replace('\u00A0', ' ')
            .Replace("/", " ")
            .Replace(".", " ");

        // collapse whitespace
        ascii = string.Join(" ", ascii.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return ascii;
    }

    private static bool IsRowEmpty(IXLRow row, int fromCol, int toCol)
    {
        foreach (var cell in row.Cells(fromCol, toCol))
        {
            if (!cell.IsEmpty())
                return false;
        }

        return true;
    }

    private static string? GetString(IXLCell? cell)
    {
        if (cell == null || cell.IsEmpty())
            return null;

        var s = cell.GetValue<string>()?.Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static int? GetInt(IXLCell? cell)
    {
        if (cell == null || cell.IsEmpty())
            return null;

        if (cell.TryGetValue<double>(out var d))
        {
            var rounded = (int)Math.Round(d, 0, MidpointRounding.AwayFromZero);
            return rounded;
        }

        var s = GetString(cell);
        if (s == null)
            return null;

        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            return i;

        return null;
    }

    private static decimal? GetDecimal(IXLCell? cell)
    {
        if (cell == null || cell.IsEmpty())
            return null;

        if (cell.TryGetValue<decimal>(out var d))
            return d;

        var s = GetString(cell);
        if (s == null)
            return null;

        // Icelandic: "1.000,50"
        if (decimal.TryParse(s, NumberStyles.Number, IsCulture, out var parsed))
            return parsed;

        var normalized = s.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            return parsed;

        return null;
    }

    private static bool? GetBool(IXLCell? cell)
    {
        if (cell == null || cell.IsEmpty())
            return null;

        if (cell.TryGetValue<double>(out var d))
            return Math.Abs(d) > double.Epsilon;

        var s = GetString(cell);
        if (s == null)
            return null;

        var lower = s.Trim().ToLowerInvariant();
        return lower switch
        {
            "já" => true,
            "ja" => true,
            "yes" => true,
            "true" => true,
            "nei" => false,
            "no" => false,
            "false" => false,
            _ => null
        };
    }

    private static DateTime? GetDateTime(IXLCell? cell)
    {
        if (cell == null || cell.IsEmpty())
            return null;

        if (cell.TryGetValue<DateTime>(out var dt))
        {
            if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            else if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();

            return dt;
        }

        var s = GetString(cell);
        if (s == null)
            return null;

        var formats = new[]
        {
            "d.M.yyyy H:mm:ss",
            "d.M.yyyy H:mm",
            "dd.MM.yyyy HH:mm:ss",
            "dd.MM.yyyy HH:mm",
            "d.M.yyyy",
            "dd.MM.yyyy"
        };

        if (DateTime.TryParseExact(s, formats, IsCulture, DateTimeStyles.AllowWhiteSpaces, out dt) ||
            DateTime.TryParse(s, IsCulture, DateTimeStyles.AllowWhiteSpaces, out dt))
        {
            if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            else if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();

            return dt;
        }

        return null;
    }
}


