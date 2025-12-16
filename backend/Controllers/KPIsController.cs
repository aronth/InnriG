using InnriGreifi.API.Data;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KPIsController : ControllerBase
{
    private readonly AppDbContext _context;

    public KPIsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetKPIs()
    {
        var currentYear = DateTime.UtcNow.Year;
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        
        // KPI 1: Product List Completeness
        var totalSuppliers = await _context.Suppliers.CountAsync();
        var suppliersWithProducts = await _context.Suppliers
            .Where(s => s.Products.Any())
            .CountAsync();
        var productListCompleteness = totalSuppliers > 0
            ? (decimal)suppliersWithProducts / totalSuppliers * 100
            : 100;
        
        // KPI 2: Stale Products (products without invoices in last 6 months)
        var totalProducts = await _context.Products.CountAsync();
        var productsWithRecentInvoices = await _context.Products
            .Where(p => p.InvoiceItems.Any(ii => ii.Invoice.InvoiceDate >= sixMonthsAgo))
            .CountAsync();
        var productsWithoutRecentInvoices = totalProducts - productsWithRecentInvoices;
        var staleProductsPercent = totalProducts > 0
            ? (decimal)productsWithoutRecentInvoices / totalProducts * 100
            : 0;
        
        // KPI 3: Quarterly Update Frequency
        var suppliers = await _context.Suppliers
            .Select(s => new
            {
                SupplierId = s.Id,
                SupplierName = s.Name,
                LastInvoiceDate = s.Invoices
                    .OrderByDescending(i => i.InvoiceDate)
                    .Select(i => (DateTime?)i.InvoiceDate)
                    .FirstOrDefault()
            })
            .ToListAsync();
        
        // Count suppliers updated in each quarter of current year
        // Ensure all dates are UTC to match database storage
        var q1Start = new DateTime(currentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var q1End = new DateTime(currentYear, 3, 31, 23, 59, 59, DateTimeKind.Utc);
        var q2Start = new DateTime(currentYear, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var q2End = new DateTime(currentYear, 6, 30, 23, 59, 59, DateTimeKind.Utc);
        var q3Start = new DateTime(currentYear, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var q3End = new DateTime(currentYear, 9, 30, 23, 59, 59, DateTimeKind.Utc);
        var q4Start = new DateTime(currentYear, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var q4End = new DateTime(currentYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        
        var q1Updated = suppliers.Count(s => s.LastInvoiceDate.HasValue && 
            s.LastInvoiceDate.Value >= q1Start && s.LastInvoiceDate.Value <= q1End);
        var q2Updated = suppliers.Count(s => s.LastInvoiceDate.HasValue && 
            s.LastInvoiceDate.Value >= q2Start && s.LastInvoiceDate.Value <= q2End);
        var q3Updated = suppliers.Count(s => s.LastInvoiceDate.HasValue && 
            s.LastInvoiceDate.Value >= q3Start && s.LastInvoiceDate.Value <= q3End);
        var q4Updated = suppliers.Count(s => s.LastInvoiceDate.HasValue && 
            s.LastInvoiceDate.Value >= q4Start && s.LastInvoiceDate.Value <= q4End);
        
        var quarterlyUpdateRate = totalSuppliers > 0
            ? ((decimal)(q1Updated + q2Updated + q3Updated + q4Updated) / (totalSuppliers * 4)) * 100
            : 0;
        
        // KPI 4: Usage Metrics (invoices processed)
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = monthStart.AddMonths(-1);
        
        var invoicesThisMonth = await _context.Invoices
            .Where(i => i.CreatedAt >= monthStart)
            .CountAsync();
        
        var invoicesLastMonth = await _context.Invoices
            .Where(i => i.CreatedAt >= lastMonthStart && i.CreatedAt < monthStart)
            .CountAsync();
        
        var result = new KPIsDto
        {
            // KPI 1: Product List Completeness
            ProductListCompleteness = new KPIValue
            {
                Current = productListCompleteness,
                Target = 100,
                Status = productListCompleteness >= 100 ? "Met" : "Ekki metið",
                Details = $"{suppliersWithProducts} af {totalSuppliers} birgjum með vörulista"
            },
            
            // KPI 2: Stale Products (inverted - higher is better)
            StaleProducts = new KPIValue
            {
                Current = 100 - staleProductsPercent, // Invert: higher is better
                Target = 100,
                Status = staleProductsPercent == 0 ? "Met" : "Ekki metið",
                Details = $"{productsWithoutRecentInvoices} af {totalProducts} vörum án reikninga síðustu 6 mánuði"
            },
            
            // KPI 3: Quarterly Update Frequency
            QuarterlyUpdateFrequency = new KPIValue
            {
                Current = quarterlyUpdateRate,
                Target = 100, // 4x per year = 100% of suppliers updated each quarter
                Status = quarterlyUpdateRate >= 100 ? "Met" : "Ekki metið",
                Details = $"Q1: {q1Updated}, Q2: {q2Updated}, Q3: {q3Updated}, Q4: {q4Updated} af {totalSuppliers} birgjum"
            },
            
            // KPI 4: Usage Metrics
            UsageMetrics = new KPIValue
            {
                Current = invoicesThisMonth, // Proxy metric
                Target = 0, // Not applicable for this metric
                Status = "Árangur",
                Details = $"{invoicesThisMonth} reikningar vinnslu í þessum mánuði (vs {invoicesLastMonth} síðasta mánuð)"
            },
            
            QuarterlyBreakdown = new QuarterlyBreakdownDto
            {
                Q1 = new QuarterStatusDto
                {
                    Updated = q1Updated,
                    Total = totalSuppliers,
                    Percentage = totalSuppliers > 0 ? (decimal)q1Updated / totalSuppliers * 100 : 0
                },
                Q2 = new QuarterStatusDto
                {
                    Updated = q2Updated,
                    Total = totalSuppliers,
                    Percentage = totalSuppliers > 0 ? (decimal)q2Updated / totalSuppliers * 100 : 0
                },
                Q3 = new QuarterStatusDto
                {
                    Updated = q3Updated,
                    Total = totalSuppliers,
                    Percentage = totalSuppliers > 0 ? (decimal)q3Updated / totalSuppliers * 100 : 0
                },
                Q4 = new QuarterStatusDto
                {
                    Updated = q4Updated,
                    Total = totalSuppliers,
                    Percentage = totalSuppliers > 0 ? (decimal)q4Updated / totalSuppliers * 100 : 0
                }
            }
        };
        
        return Ok(result);
    }
}

