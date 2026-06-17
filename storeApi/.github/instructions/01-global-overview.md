# Global Overview & Onboarding

Summary
- ChineseAuction.Api is an ASP.NET Core Web API implementing an auction/store backend (gifts, donors, orders, lottery winners, users). It exposes REST endpoints under `Controllers/`, persists state via EF Core (`AppDbContext`), and uses AutoMapper for DTO mapping.

Tech stack
- .NET 8 (TargetFramework: `net8.0`) - ASP.NET Core Web API
- Entity Framework Core 9 (SqlServer provider)
- AutoMapper
- Serilog (file sink)
- Swashbuckle/Swagger for API docs

Quick local dev commands
- Restore/build: `dotnet restore && dotnet build` (run from repository root)
- Run: `dotnet run --project ChineseAuction.Api/ChineseAuction.Api.csproj`
- EF migrations: `dotnet ef migrations add Name --project ChineseAuction.Api --startup-project ChineseAuction.Api` and `dotnet ef database update`

Global coding guidelines
- Follow C# conventions and `nullable` enabled: prefer explicit nullability and avoid suppressing warnings.
- Use `async`/`await` throughout I/O paths; expose `Task`/`Task<T>` APIs and prefer `CancellationToken` on long-running ops.
- Prefer constructor injection for dependencies and register services in DI in `Program.cs`.
- Map domain models → DTOs using AutoMapper profiles (folder: `Mappings/`). Controllers accept/return DTOs, not EF entities.
- Centralize error handling in `Middleware/ExceptionMiddleware.cs` — throw meaningful exceptions in services and let middleware shape HTTP responses.
- Use Serilog for structured logs; write meaningful messages at appropriate levels.

Folder structure (high-level)
- `Controllers/` — API endpoints
- `Services/` — business logic and orchestration
- `Repositories/` — data access wrappers around EF Core
- `Data/` — `AppDbContext` and migrations
- `Dtos/` — request/response shapes
- `Mappings/` — AutoMapper profiles
- `Middleware/` — custom middleware (exceptions, rate limiting, request logging)
- `Models/` — EF domain models

Onboarding reading order
1. `Program.cs` — DI bindings, middleware order
2. `AppDbContext.cs` — schema and DbSets
3. `Mappings/` profiles — mapping rules
4. Representative controller + service pair (e.g., `GiftController.cs` + `GiftService.cs`)
