# InnriGreifi

Business operations data drilling system for invoice processing, product catalog management, and supplier analysis.

## Tech Stack

- **Backend**: .NET 9 Web API with PostgreSQL
- **Frontend**: Nuxt 4 (Vue 3) with TypeScript and Tailwind CSS
- **Database**: PostgreSQL 15
- **Containerization**: Docker & Docker Compose

## Quick Start

### Using Docker Compose (Recommended)

```bash
docker-compose up
```

- Backend API: `http://localhost:5000`
- Frontend: `http://localhost:3000`
- PostgreSQL: `localhost:5432`

### Local Development

**Backend:**
```bash
cd backend
dotnet restore
dotnet run
```

**Frontend:**
```bash
cd frontend
npm install
npm run dev
```

**Database:**
Ensure PostgreSQL is running and update `appsettings.json` with your connection string.

## Project Structure

```
InnriGreifi/
├── backend/          # .NET 9 Web API
├── frontend/         # Nuxt 4 application
├── tests/            # Unit tests
└── docker-compose.yml
```

## Documentation

- **[agents.md](./agents.md)** - Comprehensive project rules, conventions, and guidelines for developers and AI agents
  - Architecture patterns
  - Code conventions
  - API documentation
  - Database schema
  - Business logic rules
  - Development workflow

## Features

- 📄 HTML invoice parsing (Icelandic invoices)
- 📦 Product catalog management
- 🏢 Supplier management
- 💰 Price history tracking
- 📊 Cross-supplier price comparison
- ✏️ Invoice review and editing before confirmation

## Development

See [agents.md](./agents.md) for detailed development guidelines, code conventions, and best practices.

---

**Note for AI Agents**: Please read [agents.md](./agents.md) before making changes to understand project patterns and conventions.

