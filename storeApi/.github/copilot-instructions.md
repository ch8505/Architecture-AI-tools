# Repo Onboarding for Coding Agents

Quick directive
- Read these four focused documents on first load: `instructions/01-global-overview.md`, `instructions/02-controllers-rules.md`, `instructions/03-services-rules.md`, `instructions/04-repositories-rules.md` (files live in `.github/instructions/`). They summarize tech, conventions, and patterns.

What to do on first run
1. Inspect `Program.cs` and `AppDbContext.cs` to learn service registrations and middleware order.
2. Build once: `dotnet restore && dotnet build`.
3. Run the app locally: `dotnet run --project ChineseAuction.Api/ChineseAuction.Api.csproj` and browse Swagger (`/swagger` if enabled).

Safe editing rules
- Keep controller actions thin; implement business logic in `Services/` and data access in `Repositories/`.
- Prefer creating small, focused unit tests for new logic.
- When adding new DB schema, create an EF migration and run `dotnet ef database update`.

If you fail to build
- Re-run `dotnet restore`; check `ChineseAuction.Api.csproj` for package versions.
- Confirm .NET SDK `8.0` is installed locally. Use `dotnet --list-sdks`.

Contacts & hints
- Logs are written to `logs/` (Serilog). Use them when debugging runtime behavior.
