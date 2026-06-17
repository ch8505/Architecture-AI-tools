# Repositories / Data Layer — Rules & Patterns

DbContext usage
- `AppDbContext` is injected via DI. Do NOT `new` or `Dispose` the context manually. Use per-request scoped lifetime (default for EF Core).
- Prefer `async` EF Core methods (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`). Pass `CancellationToken` when available.

Queries & performance
- Keep repository methods focused (single responsibility). Use projection (`Select`) to DTOs when returning lists to avoid over-fetching.
- For complex queries prefer `AsNoTracking()` when entity tracking is unnecessary.

Transactions & concurrency
- Use `BeginTransactionAsync` on `DbContext.Database` for multi-repository atomic operations. Handle and rethrow meaningful exceptions for middleware.

EF migrations
- Migrations live under `Migrations/`. Use `dotnet ef` commands from repo root; target the project with `--project ChineseAuction.Api`.

Testing & seeding
- Seed data via dedicated seeder classes or migration-based seed methods for integration tests. Use an in-memory provider for fast unit tests if needed, but prefer real provider in integration tests.

Safety
- Avoid raw SQL unless necessary; if used, parameterize inputs to prevent injection.
