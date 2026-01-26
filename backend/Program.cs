using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddOpenApi();

// Memory Cache for caching scraped data
builder.Services.AddMemoryCache();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .LogTo(_ => { }, LogLevel.None)); // Disable EF Core SQL query logging

// Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
    
    // User settings
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "InnriGreifi.Auth";
    
    // For development (different ports = cross-origin), use SameSite.None
    // For production (same origin), use SameSite.Lax for better security
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SameSite = SameSiteMode.None;
        // SameSite.None requires Secure flag (HTTPS only) per browser security requirements
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
    else
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require HTTPS in production
    }
    
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/"; // Ensure cookie is available for all paths
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/api/auth/login";
    // Ensure cookie persists across browser sessions
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthorization();

// Services
var corsOrigins = builder.Configuration["Cors:Origins"] ?? "http://localhost:3000";
var allowedOrigins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(o => o.Trim())
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Configure OrderLateRules
builder.Services.Configure<InnriGreifi.API.Models.OrderLateRulesOptions>(
    builder.Configuration.GetSection(InnriGreifi.API.Models.OrderLateRulesOptions.SectionName));

builder.Services.AddScoped<IInvoiceParser, HtmlInvoiceParser>();
builder.Services.AddScoped<ISupplierProductService, SupplierProductService>();
builder.Services.AddScoped<IOrderImportService, OrderImportService>();
builder.Services.AddScoped<IOrderReportingService, OrderReportingService>();
builder.Services.AddScoped<IGiftCardNumberService, GiftCardNumberService>();
builder.Services.AddScoped<IGiftCardService, GiftCardService>();
builder.Services.AddScoped<IGiftCardPdfService, GiftCardPdfService>();
builder.Services.AddScoped<IGiftCardBackgroundService, GiftCardBackgroundService>();
builder.Services.AddScoped<OrderLateRules>();

// Email Receptionist Services
// Register email services conditionally - they will handle missing configuration gracefully
builder.Services.AddScoped<IGraphEmailService, GraphEmailService>();
builder.Services.AddScoped<IEmailClassificationService, EmailClassificationService>();
builder.Services.AddScoped<IEmailClassificationQueueService, EmailClassificationQueueService>();

// Only register background services if email is configured
if (EmailConfigurationHelper.IsEmailConfigured(builder.Configuration))
{
    builder.Services.AddHostedService<EmailPollingBackgroundService>();
    builder.Services.AddSingleton<EmailClassificationBackgroundService>();
    builder.Services.AddHostedService(provider => provider.GetRequiredService<EmailClassificationBackgroundService>());
}
else
{
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Program>();
    logger.LogWarning("Email configuration is incomplete. Email polling and classification background services will not be started.");
}

// Workflow Services
builder.Services.AddScoped<IWorkflowExecutionService, WorkflowExecutionService>();
builder.Services.AddScoped<InnriGreifi.API.Services.Steps.OrderLookupStepHandler>();
builder.Services.AddScoped<InnriGreifi.API.Services.Steps.OrderVerificationStepHandler>();
builder.Services.AddScoped<InnriGreifi.API.Services.Steps.CreditCalculationStepHandler>();
builder.Services.AddScoped<InnriGreifi.API.Services.Steps.ResponseDraftStepHandler>();
builder.Services.AddScoped<InnriGreifi.API.Services.Steps.ApprovalStepHandler>();
builder.Services.AddScoped<InnriGreifi.API.Services.Steps.CreditIssuanceStepHandler>();
builder.Services.AddScoped<InnriGreifi.API.Services.Steps.EmailSendStepHandler>();

// HttpClient for web scraping and Pushover
builder.Services.AddHttpClient<IWaitTimeScraper, WaitTimeScraper>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        
        // Configure SSL certificate validation based on appsettings
        var allowInvalidCertificates = builder.Configuration.GetValue<bool>("WaitTimeScraper:AllowInvalidCertificates", false);
        
        if (allowInvalidCertificates)
        {
            // Allow invalid certificates (e.g., name mismatch, self-signed)
            // This is useful for scraping sites with certificate issues
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }
        
        return handler;
    });
builder.Services.AddHttpClient<IPushoverService, PushoverService>();
builder.Services.AddHttpClient<IBookingsScraper, BookingsScraper>();
builder.Services.AddHttpClient<IGreifinnOrderScraper, GreifinnOrderScraper>();
builder.Services.AddHttpClient<ITableBookingService, TableBookingService>();

// Wait time monitoring services (registered via HttpClient above)
// NOTE: WaitTimeMonitoringService is on hold - feature disabled
builder.Services.AddScoped<IWaitTimeScraper, WaitTimeScraper>();
builder.Services.AddScoped<IPushoverService, PushoverService>();
builder.Services.AddScoped<IBookingsScraper, BookingsScraper>();
builder.Services.AddScoped<IGreifinnOrderScraper, GreifinnOrderScraper>();
builder.Services.AddScoped<ITableBookingService, TableBookingService>();
// builder.Services.AddHostedService<WaitTimeMonitoringService>();

var app = builder.Build();

// Log SSL certificate validation setting
var allowInvalidCertificates = app.Configuration.GetValue<bool>("WaitTimeScraper:AllowInvalidCertificates", false);
if (allowInvalidCertificates)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("WaitTimeScraper: SSL certificate validation is DISABLED. This should only be enabled in development.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed database
await SeedDatabaseAsync(app);

// Register workflows
WorkflowRegistry.RegisterWorkflow(CreditIssuanceWorkflowDefinition.GetDefinition());

app.Run();

async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Get pending migrations
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var pendingMigrationsList = pendingMigrations.ToList();
        
        if (pendingMigrationsList.Any())
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", 
                pendingMigrationsList.Count, 
                string.Join(", ", pendingMigrationsList));
            await context.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully");
        }
        else
        {
            logger.LogInformation("No pending migrations");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations");
        throw;
    }
    
    // Seed roles
    var roles = new[] { "Admin", "Manager", "User" };
    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            logger.LogInformation("Seeding role: {RoleName}...", roleName);
            var role = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to seed role {RoleName}: {Errors}", 
                    roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            else
            {
                logger.LogInformation("Role {RoleName} seeded successfully", roleName);
            }
        }
    }
    
    // Seed default user
    if (!await userManager.Users.AnyAsync())
    {
        logger.LogInformation("Seeding default user...");
        var defaultUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "InnriG",
            Name = "InnriG",
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var result = await userManager.CreateAsync(defaultUser, "1234");
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed default user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        else
        {
            // Assign Admin role to the default user
            var roleResult = await userManager.AddToRoleAsync(defaultUser, "Admin");
            if (!roleResult.Succeeded)
            {
                logger.LogError("Failed to assign Admin role to default user: {Errors}", 
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
            else
            {
                logger.LogInformation("Default user seeded successfully with Admin role");
            }
        }
    }
    else
    {
        // Upgrade existing users with roles
        logger.LogInformation("Checking existing users for role assignments...");
        
        var allUsers = await userManager.Users.ToListAsync();
        foreach (var user in allUsers)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            
            // If user has no roles, assign User role by default
            if (!userRoles.Any())
            {
                logger.LogInformation("User {Username} has no roles, assigning default User role", user.UserName);
                await userManager.AddToRoleAsync(user, "User");
            }
            
            // Upgrade InnriG user to Admin if not already
            if (user.UserName == "InnriG" && !userRoles.Contains("Admin"))
            {
                logger.LogInformation("Upgrading InnriG user to Admin role");
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }
        
        logger.LogInformation("User role assignments completed");
    }
    
    // Assign default buyer to invoices without buyer
    var invoicesWithoutBuyer = await context.Invoices
        .Where(i => i.BuyerId == null)
        .ToListAsync();
    
    if (invoicesWithoutBuyer.Any())
    {
        logger.LogInformation("Found {Count} invoice(s) without buyer ID", invoicesWithoutBuyer.Count);
        
        var defaultBuyer = await context.Buyers
            .FirstOrDefaultAsync(b => b.TaxId == "4411110370");
        
        if (defaultBuyer != null)
        {
            logger.LogInformation("Found buyer with TaxId 4411110370: {BuyerName} (ID: {BuyerId})", 
                defaultBuyer.Name, defaultBuyer.Id);
            
            foreach (var invoice in invoicesWithoutBuyer)
            {
                invoice.BuyerId = defaultBuyer.Id;
            }
            
            await context.SaveChangesAsync();
            logger.LogInformation("Assigned buyer to {Count} invoice(s)", invoicesWithoutBuyer.Count);
        }
        else
        {
            logger.LogWarning("Buyer with TaxId 4411110370 not found. Cannot assign buyer to invoices.");
        }
    }
    else
    {
        logger.LogInformation("No invoices without buyer ID found");
    }
    
    // Seed gift card number sequence
    if (!await context.GiftCardNumberSequences.AnyAsync())
    {
        logger.LogInformation("Seeding gift card number sequence...");
        var sequence = new GiftCardNumberSequence
        {
            Id = Guid.NewGuid(),
            Prefix = "GC-",
            NextNumber = 1,
            NumberLength = 6,
            UpdatedAt = DateTime.UtcNow
        };
        context.GiftCardNumberSequences.Add(sequence);
        await context.SaveChangesAsync();
        logger.LogInformation("Gift card number sequence seeded successfully");
    }
    
    // Seed restaurants (using same GUIDs as migration for consistency)
    var greifinnId = new Guid("11111111-1111-1111-1111-111111111111");
    var spretturinnId = new Guid("22222222-2222-2222-2222-222222222222");
    
    var greifinn = await context.Restaurants.FirstOrDefaultAsync(r => r.Code == "GRE");
    if (greifinn == null)
    {
        logger.LogInformation("Seeding Greifinn restaurant...");
        greifinn = new Restaurant
        {
            Id = greifinnId,
            Name = "Greifinn",
            Code = "GRE",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Restaurants.Add(greifinn);
        await context.SaveChangesAsync();
        logger.LogInformation("Greifinn restaurant seeded successfully");
    }
    
    var spretturinn = await context.Restaurants.FirstOrDefaultAsync(r => r.Code == "SPR");
    if (spretturinn == null)
    {
        logger.LogInformation("Seeding Spretturinn restaurant...");
        spretturinn = new Restaurant
        {
            Id = spretturinnId,
            Name = "Spretturinn",
            Code = "SPR",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Restaurants.Add(spretturinn);
        await context.SaveChangesAsync();
        logger.LogInformation("Spretturinn restaurant seeded successfully");
    }
}
