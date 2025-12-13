# InnriGreifi - Agent Rules & Project Guidelines

## Project Overview

**InnriGreifi** is a business operations data drilling system focused on invoice processing, product catalog management, and supplier analysis. The system processes Icelandic HTML invoices, extracts product information, tracks pricing history, and enables cross-supplier price comparisons.

### Tech Stack

- **Backend**: .NET 9 Web API
- **Database**: PostgreSQL 15
- **Frontend**: Nuxt 4 (Vue 3) with TypeScript
- **Styling**: Tailwind CSS
- **ORM**: Entity Framework Core 9.0
- **HTML Parsing**: HtmlAgilityPack
- **Containerization**: Docker & Docker Compose

---

## Architecture Patterns

### Backend Architecture

1. **Layered Architecture**
   - `Controllers/` - API endpoints (thin controllers)
   - `Services/` - Business logic and domain services
   - `Models/` - Domain entities and DTOs
   - `Data/` - EF Core DbContext and migrations

2. **Dependency Injection**
   - All services registered in `Program.cs`
   - Use constructor injection
   - Services are scoped (per request)

3. **Service Pattern**
   - Business logic in services, not controllers
   - Interfaces for services (e.g., `IInvoiceParser`, `ISupplierProductService`)
   - Controllers delegate to services

### Frontend Architecture

1. **Nuxt 3 File-Based Routing**
   - Pages in `app/pages/`
   - Components in `app/components/`
   - Composables in `app/composables/` for reusable logic
   - Types in `types/` directory

2. **Composition API**
   - Use `<script setup>` syntax
   - Prefer composables over mixins
   - TypeScript for type safety

3. **API Communication**
   - Use `$fetch` for API calls
   - API base URL from `runtimeConfig.public.apiBase`
   - Composables wrap API calls (e.g., `useProducts`, `useSuppliers`)

---

## Code Conventions

### C# Backend

1. **Naming Conventions**
   - Classes: `PascalCase` (e.g., `InvoiceController`)
   - Methods: `PascalCase` (e.g., `GetInvoice`)
   - Properties: `PascalCase` (e.g., `InvoiceNumber`)
   - Private fields: `_camelCase` (e.g., `_context`)
   - Interfaces: `I` prefix (e.g., `IInvoiceParser`)

2. **File Organization**
   - One class per file
   - Namespace matches folder structure
   - DTOs in `Models/DTOs/` subfolder

3. **Async/Await**
   - Always use async/await for database operations
   - Method names end with `Async` (e.g., `GetInvoiceAsync`)
   - Use `Task<T>` return types

4. **Error Handling**
   - Return appropriate HTTP status codes
   - Use `BadRequest()` for validation errors
   - Use `NotFound()` for missing resources
   - Catch exceptions and return meaningful error messages

5. **Entity Framework**
   - Use `Include()` for eager loading
   - Use `AsQueryable()` for dynamic queries
   - Configure relationships in `OnModelCreating`
   - Use decimal precision for money: `HasPrecision(18, 2)`

### TypeScript/Vue Frontend

1. **Naming Conventions**
   - Components: `PascalCase.vue` (e.g., `InvoiceReview.vue`)
   - Composables: `camelCase.ts` with `use` prefix (e.g., `useProducts.ts`)
   - Variables: `camelCase`
   - Types/Interfaces: `PascalCase`

2. **Component Structure**
   ```vue
   <template>
     <!-- HTML -->
   </template>
   
   <script setup lang="ts">
   // TypeScript logic
   </script>
   
   <style scoped>
   /* Styles */
   </style>
   ```

3. **Reactivity**
   - Use `ref()` for primitives
   - Use `reactive()` for objects (sparingly)
   - Use `computed()` for derived state
   - Use `watch()` for side effects

4. **API Calls**
   - Always use `$fetch` from Nuxt
   - Handle errors with try/catch
   - Show loading states during async operations
   - Use composables to encapsulate API logic

---

## Database Schema

### Entity Relationships

```
Supplier (1) ──→ (N) Product
Supplier (1) ──→ (N) Invoice
Invoice (1) ──→ (N) InvoiceItem
Product (1) ──→ (N) InvoiceItem
```

### Key Constraints

1. **Supplier**
   - `Name` is unique (indexed)
   - Cannot be deleted if has Products or Invoices (Restrict)

2. **Product**
   - Compound unique key: `(SupplierId, ProductCode)`
   - Foreign key to Supplier (Restrict on delete)
   - Cannot be deleted if has InvoiceItems (Restrict)

3. **Invoice**
   - Foreign key to Supplier (Restrict on delete)
   - `TotalAmount` has precision (18, 2)

4. **InvoiceItem**
   - Foreign key to Invoice (Cascade on delete)
   - Foreign key to Product (Restrict on delete)
   - All decimal fields have precision (18, 2)
   - `Quantity` has precision (18, 3)

### Important Fields

- **All entities**: `Id` (Guid), `CreatedAt` (DateTime UTC), `UpdatedAt` (DateTime UTC)
- **Invoice**: `InvoiceNumber` (string), `InvoiceDate` (DateTime)
- **Product**: `ProductCode` (string, max 100), `Name` (string, max 300)
- **InvoiceItem**: `UnitPrice` (discounted), `ListPrice` (original), `Discount` (amount)

---

## API Conventions

### Endpoint Patterns

1. **RESTful Routes**
   - `GET /api/{resource}` - List all
   - `GET /api/{resource}/{id}` - Get by ID
   - `GET /api/{resource}/{id}/history` - Get history (products)
   - `GET /api/{resource}/compare?productCode={code}` - Compare prices
   - `POST /api/{resource}` - Create
   - `POST /api/{resource}/upload` - Upload file (invoices)
   - `POST /api/{resource}/confirm` - Confirm action (invoices)
   - `PUT /api/{resource}/{id}` - Update
   - `DELETE /api/{resource}/{id}` - Delete

2. **Query Parameters**
   - Filtering: `?supplierId={guid}`
   - Use `[FromQuery]` attribute

3. **Request/Response**
   - Use DTOs for complex requests (`InvoiceConfirmDto`)
   - Return domain models or DTOs
   - Use `[FromBody]` for POST/PUT requests
   - Use `IFormFile` for file uploads

4. **CORS**
   - Configured for `http://localhost:3000` in development
   - Policy name: `AllowFrontend`

### Response Codes

- `200 OK` - Success
- `201 Created` - Resource created (use `CreatedAtAction`)
- `204 No Content` - Success with no body (DELETE, PUT)
- `400 Bad Request` - Validation errors or bad input
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server errors

---

## Business Logic Rules

### Invoice Processing

1. **Two-Step Process**
   - Step 1: `POST /api/invoices/upload` - Parse and return for review
   - Step 2: `POST /api/invoices/confirm` - Save to database

2. **Invoice Parsing**
   - Uses `HtmlInvoiceParser` with sub-parsers
   - Sub-parsers: `TableInvoiceSubParser`, `DivInvoiceSubParser`
   - Extracts: Supplier name, invoice date, invoice number, total amount, line items
   - Handles Icelandic date format: `dd.MM.yyyy`
   - Handles Icelandic number format: `1.000,00` (dots as thousands, comma as decimal)

3. **Supplier Creation**
   - Auto-creates supplier if not exists (via `SupplierProductService`)
   - Supplier name is cleaned (removes addresses, extra punctuation)
   - Supplier name must be unique

4. **Product Creation**
   - Auto-creates product if not exists (via `SupplierProductService`)
   - Product uniqueness: `(SupplierId, ProductCode)`
   - Updates product name/unit if changed on subsequent invoices

5. **Invoice Item Mapping**
   - Each `InvoiceItem` links to a `Product`
   - `ItemId` field stores the product code
   - `ItemName` is the display name (may be cleaned)

### Product Management

1. **Price Tracking**
   - Latest price from most recent `InvoiceItem` by `InvoiceDate`
   - Price history available via `/api/products/{id}/history`
   - Tracks: `ListPrice`, `UnitPrice`, `Discount`, `DiscountPercentage`

2. **Price Comparison**
   - `/api/products/compare?productCode={code}` finds products with same code across suppliers
   - Returns latest price for each supplier
   - Sorted by price (lowest first)

3. **Product Updates**
   - Name and unit can be updated
   - `UpdatedAt` timestamp is maintained
   - Cannot delete product if referenced by InvoiceItems

### Data Validation

1. **Required Fields**
   - Supplier: `Name`
   - Product: `SupplierId`, `ProductCode`, `Name`
   - Invoice: `SupplierId`, `InvoiceNumber`, `InvoiceDate`, `TotalAmount`

2. **Decimal Precision**
   - All money fields: `decimal` with precision (18, 2)
   - Quantity: `decimal` with precision (18, 3)

3. **String Lengths**
   - Product code: max 100
   - Product name: max 300
   - Supplier name: max 200
   - Descriptions: max 1000

---

## Frontend Patterns

### Page Structure

1. **Index Page** (`pages/index.vue`)
   - Invoice upload interface
   - File dropzone component
   - Review modal with `InvoiceReview` component

2. **Product Pages**
   - `pages/products/index.vue` - Product list
   - `pages/products/[id].vue` - Product detail
   - `pages/products/catalog.vue` - Catalog view
   - `pages/products/compare.vue` - Price comparison

### Component Patterns

1. **FileDropzone**
   - Handles file selection (drag & drop or click)
   - Emits `file-selected` event with `File` object

2. **InvoiceReview**
   - Editable invoice review table
   - Props: `invoice` (Invoice type)
   - Events: `confirm` (with invoice), `cancel`
   - Auto-calculates line totals

3. **Composables**
   - `useProducts()` - Product API calls
   - `useSuppliers()` - Supplier API calls
   - Return functions, not reactive state (caller manages state)

### Styling

1. **Tailwind CSS**
   - Use utility classes
   - Responsive design with breakpoints (`sm:`, `lg:`, etc.)
   - Custom colors: indigo, purple, pink gradients

2. **Icelandic Locale**
   - Use `is-IS` locale for dates and currency
   - Currency: ISK (Icelandic Króna)
   - Date format: `dd.MM.yyyy`

---

## Important Considerations

### Date Handling

- **Backend**: Store all dates as UTC (`DateTime.UtcNow`)
- **Frontend**: Display in Icelandic locale (`is-IS`)
- **Parsing**: Handle `DateTimeKind.Unspecified` and convert to UTC

### Number Formatting

- **Icelandic Format**: `1.000,00` (dots = thousands, comma = decimal)
- **Parsing**: Remove dots, replace comma with dot for decimal parsing
- **Display**: Use `Intl.NumberFormat('is-IS')` for currency

### Error Handling

1. **Backend**
   - Catch exceptions in controllers
   - Return meaningful error messages
   - Log errors (configured in `appsettings.json`)

2. **Frontend**
   - Show user-friendly error messages in Icelandic
   - Use loading states during async operations
   - Handle network errors gracefully

### Security

1. **CORS**
   - Only allow `http://localhost:3000` in development
   - Update for production deployment

2. **File Uploads**
   - Validate file type (HTML expected)
   - Check file size limits
   - Sanitize parsed content

3. **SQL Injection**
   - Use EF Core parameterized queries (automatic)
   - Never use string concatenation for SQL

### Performance

1. **Database**
   - Use `Include()` for eager loading (avoid N+1)
   - Use `AsQueryable()` for dynamic queries
   - Index unique fields (Supplier.Name, Product compound key)

2. **Frontend**
   - Lazy load components when possible
   - Use `v-if` vs `v-show` appropriately
   - Debounce search inputs

---

## Development Workflow

### Running the Project

1. **Docker Compose** (Recommended)
   ```bash
   docker-compose up
   ```
   - Backend: `http://localhost:5000`
   - Frontend: `http://localhost:3000`
   - PostgreSQL: `localhost:5432`

2. **Local Development**
   - Backend: Run from Visual Studio/Rider or `dotnet run`
   - Frontend: `cd frontend && npm run dev`
   - Database: Ensure PostgreSQL is running

### Database Migrations

1. **Create Migration**
   ```bash
   cd backend
   dotnet ef migrations add MigrationName
   ```

2. **Apply Migration**
   ```bash
   dotnet ef database update
   ```

3. **Migrations Location**: `backend/Migrations/`

### Testing

- Test project: `tests/InnriGreifi.Tests/`
- Use xUnit for unit tests
- Test invoice parsing logic

---

## Common Tasks

### Adding a New API Endpoint

1. Create controller method in appropriate controller
2. Add route attribute: `[Route("api/[controller]")]`
3. Use appropriate HTTP verb: `[HttpGet]`, `[HttpPost]`, etc.
4. Return appropriate status code
5. Add to frontend composable if needed

### Adding a New Entity

1. Create model in `Models/`
2. Add `DbSet<T>` to `AppDbContext`
3. Configure in `OnModelCreating`
4. Create migration: `dotnet ef migrations add AddNewEntity`
5. Update database: `dotnet ef database update`

### Adding a New Frontend Page

1. Create `.vue` file in `app/pages/`
2. Use file-based routing (folder structure = route)
3. Add navigation link in `AppNav.vue` if needed
4. Use composables for API calls

### Modifying Invoice Parser

1. Sub-parsers in `Services/Parsers/`
2. Implement `IInvoiceSubParser` interface
3. Add to `HtmlInvoiceParser` constructor
4. Test with example invoices in `example_invoices/`

---

## Naming Conventions Summary

| Type | Convention | Example |
|------|------------|---------|
| C# Class | PascalCase | `InvoiceController` |
| C# Method | PascalCase | `GetInvoice` |
| C# Property | PascalCase | `InvoiceNumber` |
| C# Private Field | _camelCase | `_context` |
| C# Interface | IPascalCase | `IInvoiceParser` |
| Vue Component | PascalCase.vue | `InvoiceReview.vue` |
| TypeScript Function | camelCase | `getAllProducts` |
| TypeScript Type | PascalCase | `Invoice` |
| Database Table | PascalCase | `Invoices` |
| Database Column | PascalCase | `InvoiceNumber` |

---

## File Structure Reference

```
InnriGreifi/
├── backend/
│   ├── Controllers/          # API endpoints
│   ├── Data/                 # DbContext, migrations
│   ├── Models/               # Domain entities
│   │   └── DTOs/            # Data transfer objects
│   ├── Services/            # Business logic
│   │   └── Parsers/        # Invoice sub-parsers
│   └── Program.cs           # Application entry point
├── frontend/
│   └── app/
│       ├── components/      # Vue components
│       ├── composables/     # Reusable logic
│       ├── layouts/         # Layout components
│       ├── pages/           # Route pages
│       ├── public/          # Static assets
│       └── types/           # TypeScript types
├── docker-compose.yml        # Docker orchestration
└── agents.md                 # This file
```

---

## Notes for AI Agents

1. **Always check existing patterns** before creating new code
2. **Follow the two-step invoice process** (upload → confirm)
3. **Use services for business logic**, not controllers
4. **Maintain data integrity** (foreign keys, constraints)
5. **Handle Icelandic locale** for dates and numbers
6. **Use TypeScript types** for frontend code
7. **Test with example invoices** in `example_invoices/`
8. **Update this file** if adding new patterns or conventions

---

**Last Updated**: 2025-01-XX
**Project Version**: 1.0.0

