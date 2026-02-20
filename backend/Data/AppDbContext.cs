using InnriGreifi.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Buyer> Buyers { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<OrderImportBatch> OrderImportBatches { get; set; }
    public DbSet<OrderRow> OrderRows { get; set; }
    public DbSet<Restaurant> Restaurants { get; set; }
    public DbSet<WaitTimeNotification> WaitTimeNotifications { get; set; }
    public DbSet<WaitTimeRecord> WaitTimeRecords { get; set; }
    public DbSet<GiftCard> GiftCards { get; set; }
    public DbSet<GiftCardTemplate> GiftCardTemplates { get; set; }
    public DbSet<GiftCardNumberSequence> GiftCardNumberSequences { get; set; }
    public DbSet<EmailConversation> EmailConversations { get; set; }
    public DbSet<EmailMessage> EmailMessages { get; set; }
    public DbSet<EmailAttachment> EmailAttachments { get; set; }
    public DbSet<EmailClassificationQueue> EmailClassificationQueues { get; set; }
    public DbSet<EmailExtractedData> EmailExtractedData { get; set; }
    public DbSet<UserEmailMapping> UserEmailMappings { get; set; }
    public DbSet<UserEmailToken> UserEmailTokens { get; set; }
    public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
    public DbSet<WorkflowStepExecution> WorkflowStepExecutions { get; set; }
    public DbSet<WorkflowApproval> WorkflowApprovals { get; set; }
    public DbSet<EmailJunkFilter> EmailJunkFilters { get; set; }
    public DbSet<Menu> Menus { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingMenuItem> BookingMenuItems { get; set; }
    public DbSet<EmailClassification> EmailClassifications { get; set; }
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Supplier Configuration
        modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.Name)
            .IsUnique();
        
        // Buyer Configuration
        modelBuilder.Entity<Buyer>()
            .HasIndex(b => b.TaxId)
            .IsUnique();
        
        // Product Configuration
        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.SupplierId, p.ProductCode })
            .IsUnique();
        
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Invoice Configuration
        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Supplier)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Buyer)
            .WithMany(b => b.Invoices)
            .HasForeignKey(i => i.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => new { i.SupplierId, i.InvoiceNumber })
            .IsUnique();
        
        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalAmount)
            .HasPrecision(18, 2);
        
        // InvoiceItem Configuration
        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Product)
            .WithMany(p => p.InvoiceItems)
            .HasForeignKey(ii => ii.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.ListPrice)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.TotalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.TotalPriceWithVat)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.Discount)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.Quantity)
            .HasPrecision(18, 3);
        
        // WaitTimeNotification Configuration
        modelBuilder.Entity<WaitTimeNotification>()
            .HasOne(wtn => wtn.User)
            .WithMany(u => u.WaitTimeNotifications)
            .HasForeignKey(wtn => wtn.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<WaitTimeNotification>()
            .HasOne(wtn => wtn.Restaurant)
            .WithMany()
            .HasForeignKey(wtn => wtn.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<WaitTimeNotification>()
            .HasIndex(wtn => new { wtn.UserId, wtn.RestaurantId })
            .IsUnique();
        
        // WaitTimeRecord Configuration
        modelBuilder.Entity<WaitTimeRecord>()
            .HasOne(wtr => wtr.Restaurant)
            .WithMany()
            .HasForeignKey(wtr => wtr.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // GiftCard Configuration
        modelBuilder.Entity<GiftCard>()
            .HasIndex(gc => gc.Number)
            .IsUnique();
        
        modelBuilder.Entity<GiftCard>()
            .Property(gc => gc.Amount)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<GiftCard>()
            .HasOne(gc => gc.Template)
            .WithMany(t => t.GiftCards)
            .HasForeignKey(gc => gc.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<GiftCard>()
            .HasOne(gc => gc.CreatedBy)
            .WithMany()
            .HasForeignKey(gc => gc.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<GiftCard>()
            .HasOne(gc => gc.Restaurant)
            .WithMany()
            .HasForeignKey(gc => gc.RestaurantId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // GiftCardTemplate Configuration
        modelBuilder.Entity<GiftCardTemplate>()
            .HasOne(t => t.Restaurant)
            .WithMany()
            .HasForeignKey(t => t.RestaurantId)
            .OnDelete(DeleteBehavior.SetNull);

        // Orders (Excel imports)
        modelBuilder.Entity<OrderImportBatch>()
            .Property(b => b.FileName)
            .HasMaxLength(300);

        modelBuilder.Entity<OrderRow>()
            .HasOne(r => r.OrderImportBatch)
            .WithMany(b => b.Rows)
            .HasForeignKey(r => r.OrderImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderRow>()
            .Property(r => r.TotalAmountWithVat)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderRow>()
            .Property(r => r.DeliveryMethod)
            .HasMaxLength(20);

        modelBuilder.Entity<OrderRow>()
            .Property(r => r.OrderSource)
            .HasMaxLength(20);

        modelBuilder.Entity<OrderRow>()
            .Property(r => r.OrderTime)
            .HasColumnType("time without time zone");

        modelBuilder.Entity<OrderRow>()
            .Property(r => r.ReadyTime)
            .HasColumnType("time without time zone");

        modelBuilder.Entity<OrderRow>()
            .HasIndex(r => r.OrderNumber);

        modelBuilder.Entity<OrderRow>()
            .HasIndex(r => r.CreatedDate);

        // Restaurant Configuration
        modelBuilder.Entity<Restaurant>()
            .HasIndex(r => r.Code)
            .IsUnique();

        modelBuilder.Entity<Restaurant>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // OrderRow Restaurant relationship
        modelBuilder.Entity<OrderRow>()
            .HasOne(r => r.Restaurant)
            .WithMany()
            .HasForeignKey(r => r.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // EmailConversation Configuration
        modelBuilder.Entity<EmailConversation>()
            .HasIndex(ec => ec.GraphConversationId)
            .IsUnique();

        modelBuilder.Entity<EmailConversation>()
            .HasIndex(ec => ec.AssignedToUserId);

        modelBuilder.Entity<EmailConversation>()
            .HasIndex(ec => ec.Status);

        modelBuilder.Entity<EmailConversation>()
            .HasOne(ec => ec.AssignedTo)
            .WithMany()
            .HasForeignKey(ec => ec.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EmailConversation>()
            .HasOne(ec => ec.ExtractedData)
            .WithOne(ed => ed.Conversation)
            .HasForeignKey<EmailExtractedData>(ed => ed.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // EmailMessage Configuration
        modelBuilder.Entity<EmailMessage>()
            .HasIndex(em => em.GraphMessageId)
            .IsUnique();

        modelBuilder.Entity<EmailMessage>()
            .HasIndex(em => em.GraphConversationId);

        modelBuilder.Entity<EmailMessage>()
            .HasIndex(em => em.ConversationId);

        modelBuilder.Entity<EmailMessage>()
            .HasIndex(em => em.SentByUserId);

        modelBuilder.Entity<EmailMessage>()
            .HasOne(em => em.Conversation)
            .WithMany(ec => ec.Messages)
            .HasForeignKey(em => em.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmailMessage>()
            .HasOne(em => em.SentBy)
            .WithMany()
            .HasForeignKey(em => em.SentByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // EmailAttachment Configuration
        modelBuilder.Entity<EmailAttachment>()
            .HasOne(ea => ea.Message)
            .WithMany(em => em.Attachments)
            .HasForeignKey(ea => ea.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // EmailClassificationQueue Configuration
        modelBuilder.Entity<EmailClassificationQueue>()
            .HasIndex(eq => eq.MessageId);

        modelBuilder.Entity<EmailClassificationQueue>()
            .HasIndex(eq => eq.Status);

        modelBuilder.Entity<EmailClassificationQueue>()
            .HasIndex(eq => eq.QueuedAt);

        modelBuilder.Entity<EmailClassificationQueue>()
            .HasOne(eq => eq.Message)
            .WithMany()
            .HasForeignKey(eq => eq.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // EmailExtractedData Configuration
        modelBuilder.Entity<EmailExtractedData>()
            .HasIndex(ed => ed.ConversationId);

        modelBuilder.Entity<EmailExtractedData>()
            .HasIndex(ed => ed.MessageId);

        modelBuilder.Entity<EmailExtractedData>()
            .HasOne(ed => ed.Message)
            .WithMany()
            .HasForeignKey(ed => ed.MessageId)
            .OnDelete(DeleteBehavior.SetNull);

        // UserEmailMapping Configuration
        modelBuilder.Entity<UserEmailMapping>()
            .HasIndex(uem => uem.UserId);

        modelBuilder.Entity<UserEmailMapping>()
            .HasIndex(uem => new { uem.UserId, uem.EmailAddress })
            .IsUnique();

        modelBuilder.Entity<UserEmailMapping>()
            .HasOne(uem => uem.User)
            .WithMany()
            .HasForeignKey(uem => uem.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserEmailToken Configuration
        modelBuilder.Entity<UserEmailToken>()
            .HasIndex(uet => uet.UserId);

        modelBuilder.Entity<UserEmailToken>()
            .HasIndex(uet => new { uet.UserId, uet.EmailAddress })
            .IsUnique();

        modelBuilder.Entity<UserEmailToken>()
            .HasIndex(uet => uet.IsSystemInbox);

        modelBuilder.Entity<UserEmailToken>()
            .HasOne(uet => uet.User)
            .WithMany()
            .HasForeignKey(uet => uet.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkflowInstance Configuration
        modelBuilder.Entity<WorkflowInstance>()
            .HasIndex(wi => wi.ConversationId)
            .IsUnique();

        modelBuilder.Entity<WorkflowInstance>()
            .HasIndex(wi => wi.State);

        modelBuilder.Entity<WorkflowInstance>()
            .HasIndex(wi => wi.WorkflowType);

        modelBuilder.Entity<WorkflowInstance>()
            .HasOne(wi => wi.Conversation)
            .WithMany()
            .HasForeignKey(wi => wi.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkflowStepExecution Configuration
        modelBuilder.Entity<WorkflowStepExecution>()
            .HasIndex(wse => wse.WorkflowInstanceId);

        modelBuilder.Entity<WorkflowStepExecution>()
            .HasIndex(wse => wse.Status);

        modelBuilder.Entity<WorkflowStepExecution>()
            .HasOne(wse => wse.WorkflowInstance)
            .WithMany(wi => wi.StepExecutions)
            .HasForeignKey(wse => wse.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkflowApproval Configuration
        modelBuilder.Entity<WorkflowApproval>()
            .HasIndex(wa => wa.WorkflowInstanceId);

        modelBuilder.Entity<WorkflowApproval>()
            .HasIndex(wa => wa.Status);

        modelBuilder.Entity<WorkflowApproval>()
            .HasIndex(wa => wa.ApprovedByUserId);

        modelBuilder.Entity<WorkflowApproval>()
            .HasOne(wa => wa.WorkflowInstance)
            .WithMany(wi => wi.Approvals)
            .HasForeignKey(wa => wa.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkflowApproval>()
            .HasOne(wa => wa.ApprovedBy)
            .WithMany()
            .HasForeignKey(wa => wa.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // EmailJunkFilter Configuration
        modelBuilder.Entity<EmailJunkFilter>()
            .HasIndex(ejf => ejf.IsActive);

        modelBuilder.Entity<EmailJunkFilter>()
            .HasIndex(ejf => new { ejf.Subject, ejf.SenderEmail });

        // Menu Configuration
        modelBuilder.Entity<Menu>()
            .HasIndex(m => m.Name);

        modelBuilder.Entity<Menu>()
            .Property(m => m.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Menu>()
            .Property(m => m.ForWho)
            .HasMaxLength(100);

        modelBuilder.Entity<Menu>()
            .Property(m => m.Description)
            .HasMaxLength(1000);

        // MenuItem Configuration
        modelBuilder.Entity<MenuItem>()
            .HasOne(mi => mi.Menu)
            .WithMany(m => m.MenuItems)
            .HasForeignKey(mi => mi.MenuId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MenuItem>()
            .HasIndex(mi => mi.MenuId);

        modelBuilder.Entity<MenuItem>()
            .Property(mi => mi.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<MenuItem>()
            .Property(mi => mi.Description)
            .HasMaxLength(1000);

        modelBuilder.Entity<MenuItem>()
            .Property(mi => mi.Price)
            .HasPrecision(18, 2);

        // Customer Configuration
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Phone);

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email);

        modelBuilder.Entity<Customer>()
            .Property(c => c.Name)
            .HasMaxLength(300);

        modelBuilder.Entity<Customer>()
            .Property(c => c.Phone)
            .HasMaxLength(100);

        modelBuilder.Entity<Customer>()
            .Property(c => c.Email)
            .HasMaxLength(300);

        modelBuilder.Entity<Customer>()
            .Property(c => c.Notes)
            .HasMaxLength(1000);

        // Booking Configuration
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.BookingDate);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.Status);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.BookingDate, b.StartTime });

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Customer)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Location)
            .WithMany()
            .HasForeignKey(b => b.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Booking>()
            .Property(b => b.Status)
            .HasMaxLength(50);

        modelBuilder.Entity<Booking>()
            .Property(b => b.SpecialRequests)
            .HasMaxLength(2000);

        modelBuilder.Entity<Booking>()
            .Property(b => b.Notes)
            .HasMaxLength(2000);

        // BookingMenuItem Configuration
        modelBuilder.Entity<BookingMenuItem>()
            .HasIndex(bmi => bmi.BookingId);

        modelBuilder.Entity<BookingMenuItem>()
            .HasIndex(bmi => bmi.MenuItemId);

        modelBuilder.Entity<BookingMenuItem>()
            .HasOne(bmi => bmi.Booking)
            .WithMany(b => b.BookingMenuItems)
            .HasForeignKey(bmi => bmi.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookingMenuItem>()
            .HasOne(bmi => bmi.MenuItem)
            .WithMany()
            .HasForeignKey(bmi => bmi.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BookingMenuItem>()
            .Property(bmi => bmi.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<BookingMenuItem>()
            .Property(bmi => bmi.Notes)
            .HasMaxLength(500);

        // EmailClassification Configuration
        modelBuilder.Entity<EmailClassification>()
            .HasIndex(ec => ec.Name)
            .IsUnique();

        modelBuilder.Entity<EmailClassification>()
            .HasIndex(ec => ec.IsActive);

        modelBuilder.Entity<EmailClassification>()
            .HasIndex(ec => ec.IsSystem);

        // WorkflowDefinition Configuration
        modelBuilder.Entity<WorkflowDefinition>()
            .HasIndex(wd => wd.Name)
            .IsUnique();

        modelBuilder.Entity<WorkflowDefinition>()
            .HasIndex(wd => wd.ClassificationId);

        modelBuilder.Entity<WorkflowDefinition>()
            .HasIndex(wd => wd.IsActive);

        modelBuilder.Entity<WorkflowDefinition>()
            .HasOne(wd => wd.Classification)
            .WithMany(ec => ec.Workflows)
            .HasForeignKey(wd => wd.ClassificationId)
            .OnDelete(DeleteBehavior.SetNull);

        // WorkflowStepDefinition is not an entity - it's serialized to JSON
        // Explicitly ignore it to prevent EF Core from trying to map it
        modelBuilder.Ignore<WorkflowStepDefinition>();
    }
}
