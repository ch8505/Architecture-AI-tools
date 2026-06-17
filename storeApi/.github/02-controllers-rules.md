# Controllers / API Layer — Rules & Patterns

Routing & attributes
- Use `[ApiController]` + `[Route("api/[controller]")]` on controllers.
- Keep routes resource-oriented (e.g., `GET /api/gift`, `GET /api/gift/{id:int}`). Use route tokens and attribute routes for special actions (`/admin`, `/search`).

Signatures & responses
- Prefer `Task<ActionResult<T>>` or `Task<IActionResult>` for async endpoints.
- Return DTOs (not EF entities); use `Ok(...)`, `CreatedAtAction(...)`, `NoContent()`, `BadRequest()`, `NotFound()` as appropriate.
- Annotate common response types with `[ProducesResponseType(StatusCodes.Status200OK)]` etc. when adding new public endpoints.

Validation & model binding
- Use DTOs for input models and decorate them with data annotations; validate with `ModelState.IsValid` only if manual checks are needed (AutoValidation is enabled via `[ApiController]`).
- For file uploads use `[FromForm]` and handle files via `IFileService`.

Error handling
- Do not craft exception-to-response mappings in controllers. Throw domain exceptions (e.g., `KeyNotFoundException`) and let `ExceptionMiddleware` translate them to proper status codes and messages.

Auth & authorization
- Apply `[Authorize]` and `[Authorize(Roles = "Admin")]` at action or controller level. Keep public endpoints explicitly `[AllowAnonymous]` if needed.

Logging & telemetry
- Inject `ILogger<T>` when controller-level logs are required; avoid heavy logic in controllers.

Testing & API docs
- Keep controllers thin to make unit testing easier. Use Swagger tags where useful; controllers are auto-discovered by Swashbuckle.
