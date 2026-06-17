# Services / Business Logic — Rules & Patterns

Responsibilities
- Services contain business rules, orchestration of repositories, and transactional boundaries. They must not manipulate `HttpContext` or perform framework-level concerns.

Design
- Keep services stateless and injectable via interfaces (`ISomeService`). Accept dependencies (repositories, mappers, file services) through constructor injection.
- Methods should be `async` and return `Task`/`Task<T>`; name long-running operations clearly and document side effects.

Validation & errors
- Validate inputs at the service boundary. Throw typed exceptions for exceptional business conditions (e.g., `ArgumentException`, `InvalidOperationException`, or domain-specific exceptions). Let exception middleware map them to HTTP responses.

Mapping & DTOs
- Use AutoMapper (Profiles in `Mappings/`) to convert between domain models and DTOs. Services should return DTOs when used directly by controllers, or domain objects for internal calls.

Transactions
- When an operation requires a transaction across multiple repository calls, either use an explicit `DbContext` transaction in the service or expose a repository method that executes the atomic unit.

File operations
- Delegate file uploads/downloads to `IFileService` and store only paths/URIs in the DB.

Testing
- Keep services testable by depending on interfaces and avoiding static/global state. Mock repositories and mapper in unit tests.
