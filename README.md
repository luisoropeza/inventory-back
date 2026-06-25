# Inventory Backend API

A production-grade multi-tenant inventory management REST API built with .NET 10, following Clean Architecture principles.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Reference](#api-reference)
- [Authentication](#authentication)
- [Key Concepts](#key-concepts)
- [Testing](#testing)
- [Database](#database)

---

## Overview

This API provides inventory management capabilities including product tracking, stock movements, purchases, sales, branch and warehouse management, audit history, and multi-tenant business isolation — all secured with JWT Bearer authentication.

**Tech stack:**

| Layer | Technologies |
|---|---|
| Runtime | .NET 10 |
| Database | PostgreSQL 16+ via Npgsql EF Core 10 |
| Auth | JWT Bearer + BCrypt.Net |
| Mapping | AutoMapper 16 |
| Validation | FluentValidation 12 |
| Excel | ClosedXML |
| Docs | Swagger / Swashbuckle |
| Tests | xUnit + Moq |

---

## Architecture

The solution uses **Clean Architecture** with four layers plus a test project:

```
Inventory.Domain          ← Pure entities, enums, no external deps
Inventory.Application     ← Services, DTOs, validators, repository interfaces
Inventory.Infrastructure  ← EF Core, repositories, JWT, Excel reader, seeding
Inventory.API             ← Controllers, middleware, DI wiring (entry point)
Inventory.Tests           ← xUnit + Moq unit tests
```

**Request flow:**

```
Controller → Service → Repository → InventoryDbContext (PostgreSQL)
```

Each layer exposes a static `DependencyInjection` extension that registers its own services. `Program.cs` calls all of them.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 16+](https://www.postgresql.org/download/)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

---

## Getting Started

### 1. Clone and restore

```bash
git clone <repository-url>
cd inventory-backend
dotnet restore Inventory.sln
```

### 2. Configure the database

Update the connection string in `Inventory.API/appsettings.json` (see [Configuration](#configuration)).

Create the database and apply migrations:

```bash
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API
```

### 3. Run

```bash
dotnet run --project Inventory.API
```

The API starts on `https://localhost:5001` / `http://localhost:5000`. Swagger UI is available at `/swagger` in development.

On startup, `DatabaseSeeder` runs automatically (idempotent) and seeds:

| Entity | Count |
|---|---|
| Business | 1 |
| Roles | 2 (Admin, Seller) |
| Measures | 6 |
| Categories | 4 |
| Products | 6 |
| Warehouses | 2 |
| Branches | 2 |
| Users | 2 (`admin` / `manager`) |

### 4. Build and format

```bash
# Build
dotnet build Inventory.sln

# Check formatting (required before merging)
dotnet format Inventory.sln --verify-no-changes --severity info

# Fix formatting
dotnet format Inventory.sln --severity info
```

---

## Configuration

`Inventory.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=inventory;Username=postgres;Password=mysecretpassword"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "InventoryAPI",
    "Audience": "InventoryAPI",
    "ExpirationInMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": ["https://yourfrontend.com"]
  }
}
```

> In development, CORS allows any origin. In production, set `Cors:AllowedOrigins` in configuration.

> **Important:** Replace `SecretKey` with a strong secret (minimum 32 characters) before deploying.

---

## API Reference

All protected endpoints require:
- `Authorization: Bearer <accessToken>` header
- `businessId: <guid>` header — must match the `businessId` encoded in the JWT; requests with a mismatched value are rejected with **403 Forbidden**

### Authentication

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | None | Obtain JWT + refresh token |
| POST | `/api/auth/refresh` | None | Rotate refresh token |

### Products

| Method | Endpoint | Role | Response |
|---|---|---|---|
| GET | `/api/product` | Any | 200 `PaginatedList<ProductResponse>` |
| GET | `/api/product/{id}` | Any | 200 `ProductResponse` |
| POST | `/api/product` | Admin | 200 `ProductResponse` |
| PUT | `/api/product/{id}` | Admin | 204 |
| DELETE | `/api/product/{id}` | Admin | 204 |
| POST | `/api/product/bulk-upload` | Admin | 200 (Excel import — `businessId` header required) |
| GET | `/api/product/template` | Any | 200 (Excel template download) |

### Categories

| Method | Endpoint | Role | Response |
|---|---|---|---|
| GET | `/api/category` | Any | 200 `PaginatedList<CategoryResponse>` |
| GET | `/api/category/{id}` | Any | 200 `CategoryResponse` |
| POST | `/api/category` | Admin | 200 `CategoryResponse` |
| PUT | `/api/category/{id}` | Admin | 204 |
| DELETE | `/api/category/{id}` | Admin | 204 |

### Branches

| Method | Endpoint | Role | Response |
|---|---|---|---|
| GET | `/api/branch` | Any | 200 `PaginatedList<BranchResponse>` |
| GET | `/api/branch/{id}` | Any | 200 `BranchResponse` |
| POST | `/api/branch` | Admin | 200 `BranchResponse` |
| PUT | `/api/branch/{id}` | Admin | 204 |
| DELETE | `/api/branch/{id}` | Admin | 204 |
| GET | `/api/branch/{id}/products` | Any | 200 `PaginatedList<ProductResponse>` |
| POST | `/api/branch/{id}/products` | Any | 204 |
| PUT | `/api/branch/{id}/products` | Admin | 204 |
| DELETE | `/api/branch/{id}/products` | Admin | 204 |
| GET | `/api/branch/{id}/products/doesnt-exist` | Any | 200 (products not yet in branch) |
| POST | `/api/branch/{id}/sales` | Any | 204 (transactional) |
| GET | `/api/branch/{id}/sales` | Any | 200 `PaginatedList<SaleResponse>` |

### Warehouses

| Method | Endpoint | Role | Response |
|---|---|---|---|
| GET | `/api/warehouse` | Any | 200 `PaginatedList<WarehouseResponse>` |
| GET | `/api/warehouse/{id}` | Any | 200 `WarehouseResponse` |
| POST | `/api/warehouse` | Admin | 200 `WarehouseResponse` |
| PUT | `/api/warehouse/{id}` | Admin | 204 |
| DELETE | `/api/warehouse/{id}` | Admin | 204 |
| GET | `/api/warehouse/{id}/products` | Any | 200 `PaginatedList<WarehouseProductResponse>` |
| POST | `/api/warehouse/{id}/products` | Any | 204 |
| DELETE | `/api/warehouse/{id}/products` | Admin | 204 |
| GET | `/api/warehouse/{id}/products/doesnt-exist` | Any | 200 (products not yet in warehouse) |

### Purchases

| Method | Endpoint | Role | Response |
|---|---|---|---|
| POST | `/api/purchase` | Any | 204 (transactional) |
| GET | `/api/purchase` | Any | 200 `PaginatedList<PurchaseResponse>` |

### Inventory Movements

| Method | Endpoint | Role | Response |
|---|---|---|---|
| GET | `/api/inventorymovement` | Any | 200 `PaginatedList<InventoryMovementResponse>` |
| POST | `/api/inventorymovement` | Any | 200 `InventoryMovementResponse` |

Movement types: `Entry`, `Exit`, `Transfer` (resolved via Strategy pattern).

### Users

| Method | Endpoint | Role | Response |
|---|---|---|---|
| GET | `/api/user` | Any | 200 `PaginatedList<UserResponse>` |
| GET | `/api/user/{id}` | Any | 200 `UserResponse` |
| POST | `/api/user` | Admin | 200 `UserResponse` |
| PUT | `/api/user/{id}` | Admin | 204 |
| DELETE | `/api/user/{id}` | Admin | 204 |

### Customers

| Method | Endpoint | Role | Response |
|---|---|---|---|
| GET | `/api/customer` | Any | 200 `PaginatedList<CustomerResponse>` |
| POST | `/api/customer` | Any | 200 `CustomerResponse` |
| PUT | `/api/customer/{id}` | Any | 204 |

### Providers

| Method | Endpoint | Role | Response |
|---|---|---|---|
| GET | `/api/provider` | Any | 200 `PaginatedList<ProviderResponse>` |
| GET | `/api/provider/{id}` | Any | 200 `ProviderResponse` |
| POST | `/api/provider` | Admin | 200 `ProviderResponse` |
| PUT | `/api/provider/{id}` | Admin | 204 |
| DELETE | `/api/provider/{id}` | Admin | 204 |

### Other Endpoints

| Method | Endpoint | Role | Response |
|---|---|---|---|
| GET | `/api/business` | Admin | 200 `PaginatedList<BusinessResponse>` |
| POST | `/api/business` | Admin | 200 `BusinessResponse` |
| GET | `/api/dashboard/today` | Any | 200 `DashboardResponse` |
| GET | `/api/roles` | Any | 200 `List<RoleResponse>` |
| GET | `/api/measures` | Any | 200 `List<MeasureResponse>` |
| GET | `/api/audithistory` | Any | 200 `PaginatedList<AuditHistoryResponse>` |

---

## Authentication

The API uses JWT Bearer tokens with refresh token rotation.

### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "yourpassword"
}
```

Response:

```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<token>",
  "expiresIn": 3600
}
```

### Using the token

Include the token and your business ID in every protected request:

```http
Authorization: Bearer <accessToken>
businessId: <your-business-guid>
```

The `businessId` header is validated against the claim encoded in your JWT. Providing a different business ID returns **403 Forbidden**.

### Refresh token rotation

Access tokens expire after 60 minutes. Refresh tokens rotate every 7 days:

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "<token>"
}
```

Returns a new `accessToken` + `refreshToken` pair. The old refresh token is revoked immediately.

---

## Key Concepts

### Multi-tenancy

Every major entity is scoped to a `BusinessId`. Controllers accept `businessId` as a required request header (`[FromHeader][BindRequired]`). `BusinessIdValidationMiddleware` rejects any authenticated request where the header value doesn't match the `businessId` claim in the JWT, preventing cross-tenant access. Every repository query also filters by `businessId` as a second layer of defense.

### Password requirements

Passwords created via the API (`POST /api/user`) must satisfy:

- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

> Note: seeded users (`admin`, `manager`) were inserted directly via the database seeder, bypassing this validation. Their passwords are intentionally simple for development convenience only.

### Soft deletes

Records are never hard-deleted. Setting `IsDeleted = true` hides them from all queries via a global EF Core query filter.

### Pagination

List endpoints return `PaginatedList<T>`. Pass `pageIndex` / `pageSize` (1-based) as query parameters.

### Inventory movement strategies

Stock changes are handled via the Strategy pattern (`MovementStrategyResolver`):

- **Entry** — adds stock to a warehouse or branch
- **Exit** — removes stock from a warehouse or branch
- **Transfer** — moves stock between locations

### Stock management

`BranchProduct` and `WarehouseProduct` expose `AddStock(int)` and `ReduceStock(int)` domain methods with quantity validation. Always use these instead of assigning `Stock` directly.

### Builder pattern

All entities are constructed via builders in `Inventory.Domain/Entities/Builders/`. Never use object initializers directly on entities.

### Audit history

All write operations (purchases, sales, inventory movements) are audited automatically via `AuditHistoryService` and stored with the acting user's ID and a timestamp.

### Bulk product import

`POST /api/product/bulk-upload` accepts an `.xlsx` file and requires the `businessId` header. Every row must contain a valid numeric `CategoryId` (column 4); rows with an invalid or missing value are rejected with a 400 error. Use `GET /api/product/template` to download the expected column format before uploading.

### Error responses

All errors are handled by `ExceptionHandlingMiddleware`:

| Exception | HTTP Status |
|---|---|
| `ValidationException` | 400 (field errors in `extensions.errors`) |
| `ArgumentException` / `InvalidOperationException` | 400 |
| `KeyNotFoundException` | 404 |
| `UnauthorizedAccessException` | 401 |
| businessId header mismatch | 403 |
| Anything else | 500 |

---

## Testing

```bash
# Run all tests
dotnet test

# Run a specific test class
dotnet test --filter "FullyQualifiedName~ProviderServiceTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~ProductServiceTests.GetProductByIdAsync_ReturnsProduct_WhenExists"
```

Tests use xUnit with Moq. Each service has unit tests covering the happy path and `KeyNotFoundException` scenarios. Dependencies (`IRepository`, `IMapper`, `IValidator`) are mocked.

Test naming convention: `MethodName_Condition_ExpectedResult`.

---

## Database

### Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project Inventory.Infrastructure --startup-project Inventory.API

# Apply pending migrations
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API
```

> **Pending migration:** The `Business` table was created as `"Businesss"` (typo) in the initial migration. The model has been corrected. Run the commands above with migration name `FixBusinessesTableName` to rename the table in your database before running the application.

### ID strategy

| Entity | PK type |
|---|---|
| Category, Product, Measure, Role | `int` (auto-increment) |
| Everything else | `Guid` (`uuid_generate_v4()`) |

### Key entities

| Entity | Description |
|---|---|
| `Business` | Tenant root |
| `Branch` / `Warehouse` | Physical locations with stock |
| `BranchProduct` / `WarehouseProduct` | Junction tables tracking stock per location |
| `Purchase` / `Sale` | Transactional records with detail lines |
| `InventoryMovement` | Tracks every stock change |
| `AuditHistory` | Immutable audit log |
| `RefreshToken` | Rotation-based auth tokens |
| `BusinessSaleCounter` | Per-business sale folio numbering (no soft-delete) |
