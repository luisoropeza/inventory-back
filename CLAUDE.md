# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build Inventory.sln

# Run API (Swagger at /swagger in dev)
dotnet run --project Inventory.API

# Run all tests
dotnet test

# Run tests for a specific class
dotnet test --filter "FullyQualifiedName~ProductServiceTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~ProductServiceTests.GetProductByIdAsync_ReturnsProduct_WhenExists"

# Check formatting
dotnet format Inventory.sln --verify-no-changes --severity info

# Apply formatting
dotnet format Inventory.sln --severity info

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project Inventory.Infrastructure --startup-project Inventory.API

# Apply migrations
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API
```

## Architecture

Clean Architecture with five projects:

- **Inventory.Domain** — Pure entities, value objects, enums. No external dependencies. Uses the Builder pattern for all complex entities (ProductBuilder, SaleBuilder, etc.) — never use object initializers directly on entities.
- **Inventory.Application** — Service interfaces, DTOs, FluentValidation validators, AutoMapper profiles, repository interfaces. Strategy pattern for inventory movements (Entry/Exit/Transfer resolved by `MovementStrategyResolver`).
- **Inventory.Infrastructure** — EF Core DbContext (`InventoryDbContext`), repository implementations, JWT service, password hasher, database seeder (idempotent, runs on startup).
- **Inventory.API** — 15 ASP.NET Core controllers, middleware, `Program.cs` wiring.
- **Inventory.Tests** — xUnit + Moq unit tests for all services.

### DI Wiring

Each layer exposes a static extension method called from `Program.cs`:

```csharp
builder.Services.AddApplication();          // AutoMapper, validators, services
builder.Services.AddInfrastructure(config); // DbContext, repositories, JWT auth, health checks
```

### Multi-tenancy

Every entity is scoped to a `BusinessId`. Controllers require a `[FromHeader][BindRequired] Guid businessId` header. `BusinessIdValidationMiddleware` enforces that this header matches the `businessId` claim in the user's JWT — returning 403 on mismatch. Global EF Core query filters enforce tenant isolation at the query level. The rate limiter partitions by `businessId`.

### Authentication

JWT Bearer with refresh token rotation. Access tokens expire in 60 minutes; refresh tokens last 7 days. BCrypt is used for password hashing. Seeded credentials: `admin/admin123` (Admin) and `manager/manager123` (Seller).

JWT claims include: `sub` (user ID), `unique_name`, `roleId`, `role`, `businessId`, `email`, `jti`. The `businessId` claim is the source of truth used by `BusinessIdValidationMiddleware` and `ICurrentUserService.GetCurrentBusinessId()`.

### Middleware Stack (in order)

1. `ExceptionHandlingMiddleware` — maps `KeyNotFoundException` → 404, `UnauthorizedAccessException` → 401, `InvalidOperationException`/`ArgumentException` → 400, `ValidationException` → 400 with field-level errors (RFC 7807 ProblemDetails)
2. CORS (`MyCustomCors`) — any origin in dev; `Cors:AllowedOrigins` in production
3. Rate limiting — fixed window, 100 req/min, partitioned by `businessId` header or IP
4. JWT Authentication
5. `BusinessIdValidationMiddleware` — validates `businessId` header matches JWT claim
6. Authorization

### Data Access

- IDs: `int` (auto-increment) for Category, Product, Measure, Role; `Guid` (uuid_generate_v4) for most others
- Soft deletes on Product, Category, Branch, Warehouse, User, Customer, Provider, Location — global query filters exclude deleted records
- Composite keys on `BranchProduct` and `WarehouseProduct` (BranchId+ProductId, WarehouseId+ProductId)
- `IQuerableExtensions.cs` uses the **C# 14 extension member syntax** (`extension(IQueryable<T> source) { ... }` inside a static class) — this is valid for net10.0 and intentional, not a syntax error

### Pending Schema Migration

The `Business` table was named `"Businesss"` (typo) in the initial migration. The DbContext has been corrected to `"Businesses"`. A migration to rename the table in the database has not yet been applied. Run:

```bash
dotnet ef migrations add FixBusinessesTableName --project Inventory.Infrastructure --startup-project Inventory.API
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API
```

### Test Conventions

Tests follow the naming pattern `MethodName_Condition_ExpectedResult`. Each service test class mocks its repository, mapper, and validator via Moq. Static helper methods (`CreateProduct()`, `CreateRequest()`) generate test data. When adding a new service method with a changed signature, update its corresponding test file to match.
