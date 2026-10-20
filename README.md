# SQL Server + EF Core Pro — migrations, stored procedures, indexing, and query tuning

[![Build](https://github.com/varunkonnojutech-lab/sqlserver-efcore-pro/actions/workflows/ci.yml/badge.svg)](https://github.com/varunkonnojutech-lab/sqlserver-efcore-pro/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/varunkonnojutech-lab/sqlserver-efcore-pro)

Most EF Core tutorials stop at `DbSet` and CRUD. Production work starts after that: schema migrations under source control, the right indexes for the real query shapes, a deliberate answer to N+1, and knowing when a stored procedure or compiled query earns its complexity. This repo is a runnable SQL Server lab built around those decisions.

## Who is this for

Intermediate and senior .NET developers who already know basic EF Core and want the data-layer judgment used on real enterprise systems — especially developers preparing for roles where the job description says **SQL Server, T-SQL, EF Core, and performance tuning**.

## What you'll learn

- Modeling a small commerce schema with explicit EF configurations instead of hiding every rule in attributes
- Creating and applying a real EF Core migration, including stored procedures added through migration SQL
- Calling stored procedures safely from EF Core with parameterized `FromSqlInterpolated` and `ExecuteSqlInterpolated`
- Preventing N+1 twice: by seeing the bad pattern in code, then replacing it with `Include`, `ThenInclude`, `AsNoTracking`, and `AsSplitQuery`
- Using DTO projections and pagination instead of materializing tracked entity graphs for grids and reports
- When EF compiled queries help — and when they are premature optimization
- Designing indexes from predicates and sort order, then reading an execution plan before and after a covering index
- Keeping local infrastructure reproducible with Docker Compose and a health-checked SQL Server 2022 container

## Quick start

### Option A — everything in Docker

```bash
docker compose up --build
```

That starts SQL Server, waits for its health check, builds the API, applies migrations, creates the stored procedures, and seeds sample data.

- Swagger: <http://localhost:8080/swagger>
- Health: <http://localhost:8080/health>
- Search orders: <http://localhost:8080/api/orders/search?customerId=1&page=1&pageSize=10>
- Stored-procedure report: <http://localhost:8080/api/analytics/top-customers?take=5>

### Option B — SQL Server in Docker, .NET on your machine

```bash
docker compose up -d sqlserver

# Install the EF tool once if needed
dotnet tool install --global dotnet-ef --version 8.*

# Apply the migration (tables + indexes + stored procedures)
dotnet ef database update \
  --project src/SqlServerEfCorePro.Data \
  --startup-project src/SqlServerEfCorePro.Api

# Seed and run
dotnet run --project src/SqlServerEfCorePro.Demo -- seed
dotnet run --project src/SqlServerEfCorePro.Api
```

The default local connection targets port **14333** with the demo-only password from `docker-compose.yml`. Override it with `COMMERCE_CONNECTION_STRING` for the demo/design-time factory or `ConnectionStrings__CommerceDb` for the API. Never reuse the local password outside this lab.

## Run the tuning demos

The console app turns on EF command logging so you can count the SQL statements yourself:

```bash
# Deliberately bad: explicit loading inside loops (1 + N + more queries)
dotnet run --project src/SqlServerEfCorePro.Demo -- naive --customer 1

# Fixed: one logical split-query graph load
dotnet run --project src/SqlServerEfCorePro.Demo -- fixed --customer 1

# Paged DTO projection
dotnet run --project src/SqlServerEfCorePro.Demo -- search --customer 1

# Compiled query
dotnet run --project src/SqlServerEfCorePro.Demo -- compiled --customer 1

# Stored procedures
dotnet run --project src/SqlServerEfCorePro.Demo -- stored-procedure
dotnet run --project src/SqlServerEfCorePro.Demo -- restock --product 1 --quantity 5
```

For the execution-plan exercise, open [`database/03-query-tuning-lab.sql`](database/03-query-tuning-lab.sql) in SSMS or Azure Data Studio. It compares a function-wrapped, `SELECT *` query with a SARGable half-open date range, then adds an optional covering index and explains how to read seeks, scans, key lookups, sorts, estimated vs. actual rows, and logical reads. The seed set is intentionally small; the lab explains why a scan can still be the correct plan on tiny data instead of inventing fake benchmark numbers.

## Project layout

```text
src/
  SqlServerEfCorePro.Data/
    Configurations/        Explicit table, relationship, precision, and index rules
    Data/                  CommerceDbContext + design-time factory
    Entities/              Customer, Product, Order, OrderItem
    Migrations/            EF Core migration + model snapshot (includes procedures)
    Queries/               DTOs, paging types, compiled-query support types
    Repositories/          Naive vs fixed loaders, search, compiled query, SP calls
    Seeding/               Deterministic customers/products/orders
  SqlServerEfCorePro.Api/  Minimal APIs + Swagger; migration/seed switches for Compose
  SqlServerEfCorePro.Demo/ Console lab that makes query counts visible
tests/
  SqlServerEfCorePro.Tests/
                           Repository/model tests using SQLite's relational provider
database/
  01-create-schema.sql     Readable T-SQL mirror of the EF schema
  02-stored-procedures.sql Procedures called by the repository
  03-query-tuning-lab.sql  Before/after execution-plan exercise
docker-compose.yml         SQL Server 2022 + API
Dockerfile                 Multi-stage, non-root API image
```

## The senior-level decisions

### 1. No lazy-loading surprises

There is no lazy-loading proxy in this repo. Related data is either included explicitly or projected. That makes query cost visible in code review — especially the method named `LoadCustomerHistoryNaiveAsync`, which is a teaching counterexample, not a production pattern.

### 2. Split query is a trade-off, not a magic switch

`AsSplitQuery()` avoids repeating parent columns for every child row, but it issues more than one SQL statement and can observe data at slightly different moments. It is the right fit for this read-only order graph; a write workflow or strict consistency requirement deserves a different conversation.

### 3. Compiled queries are selective

The compiled query is stable, simple, and shaped like a hot customer-history path. Most queries stay normal LINQ so they remain composable and readable. Measure first; compile the few shapes that matter.

### 4. Offset paging is only the first step

The search endpoint demonstrates safe offset paging with a deterministic order (`OrderDateUtc`, then `Id`). For a very large, fast-moving orders feed, the next production step is keyset/seek pagination — no deep `OFFSET` scan and no skipped/duplicated rows when inserts happen between pages.

### 5. Stored procedures stay behind the repository

The API does not scatter raw procedure names across endpoints. Parameterized calls live in `OrderReadRepository`, DTOs describe the result, and the T-SQL is visible both in the migration and in `database/02-stored-procedures.sql`.

## Tests

```bash
dotnet test
```

CI restores, builds in Release, and runs the test project. Repository tests use SQLite because they verify LINQ translation and behavior in-process. They do **not** pretend to verify SQL Server stored procedures or execution plans; those are exercised through the Docker/SQL scripts above, which is the honest boundary.

## Next steps

1. Run the naive and fixed demos with EF logging and count statements for 1, 3, and 10 orders.
2. Work through the execution-plan lab, then remove the covering index and repeat it.
3. Convert order search from offset paging to keyset pagination and compare deep-page behavior.
4. Add a filtered index for active products and explain which query it serves before creating it.
5. Continue with [`dapper-vs-efcore`](https://github.com/varunkonnojutech-lab/dapper-vs-efcore) to compare this EF-first approach with hand-written SQL.
