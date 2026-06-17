# Microservices Decomposition Plan for ChineseAuction.Api

## 1. Current Monolith Overview
- The repository implements `ChineseAuction.Api`, a single ASP.NET Core Web API for an auction/store application. Features include gift/catalog management, donors, order creation and ticket purchases, lottery selection of winners, user authentication/authorization, simple AI/chat integration, and file (image) uploads. Data is persisted via EF Core (`AppDbContext`) and domain types include `Gift`, `Category`, `Donor`, `Order`/`OrderItem`, `User`, and `Winner`.
- Main responsibilities currently live in these folders: `Controllers/`, `Services/`, `Repositories/`, `Data/`, `Dtos/`, `Mappings/`, `Middleware/`, and `Models/`.

## 2. Proposed Microservices Breakdown
Below are compact service proposals based on the existing code layout and domain models.

- Service Name: `identity` (Authentication & Users)
  - Responsibility: user management, authentication, JWT issuance, roles/claims
  - Owns these entities/tables: `User` (users table, credentials, roles)
  - Exposes these endpoints: `POST /api/auth/login`, `POST /api/auth/register`, `GET /api/auth/me`, `POST /api/auth/refresh`
  - Depends on: none (base service), emits `UserRegistered` event

- Service Name: `catalog` (Gifts & Categories & Donors metadata)
  - Responsibility: manage gifts, categories, donors metadata and images (metadata only)
  - Owns these entities/tables: `Gift`, `Category`, `Donor` (and image metadata columns)
  - Exposes these endpoints: `GET /api/gifts`, `GET /api/gifts/{id}`, `GET /api/gifts/search`, `POST /api/gifts` (admin), `PUT /api/gifts/{id}` (admin), `GET /api/categories`, `GET /api/donors`
  - Depends on: `identity` (for auth/roles verification via JWT), `files` (for storing images) — occasionally calls `files` synchronously or via signed URL
  - Emits: `GiftCreated`, `GiftUpdated`

- Service Name: `orders` (Orders & Payments/Tickets)
  - Responsibility: create/persist orders, manage order items (tickets), hold payment/fulfillment state
  - Owns these entities/tables: `Order`, `OrderItem`
  - Exposes these endpoints: `POST /api/orders`, `GET /api/orders/{id}`, `GET /api/orders?userId=...`, `POST /api/orders/{id}/pay` (if payments)
  - Depends on: `identity` (user info), `catalog` (validate gifts and prices via sync call or cached copy)
  - Emits: `OrderCreated`, `OrderPaid`

- Service Name: `lottery` (Lottery draws & Winners)
  - Responsibility: run draws, manage winner selection and reporting
  - Owns these entities/tables: `Winner`, `Lottery` (if present), draw results
  - Exposes these endpoints: `POST /api/lottery/draw`, `GET /api/lottery/winners`, `GET /api/lottery/{id}`
  - Depends on: `orders` (to read ticket purchases), `catalog` (to verify gift existence)
  - Consumes events: `OrderCreated` (or `OrderPaid`) to consider eligibility
  - Emits: `WinnerSelected`

- Service Name: `files` (File / Asset Service)
  - Responsibility: store images, return URLs/signed URLs, manage lifecycle of uploaded files
  - Owns these entities/tables: file metadata (path, owner, createdAt)
  - Exposes these endpoints: `POST /api/files/upload`, `GET /api/files/{id}`, `DELETE /api/files/{id}`
  - Depends on: `identity` for access control
  - Storage: backs to blob storage or shared filesystem (S3/Azure Blob recommended)

- Service Name: `ai` (AI / Chat integrations)
  - Responsibility: host AI prompts, chat history DTOs, orchestrate external LLM calls (AiService)
  - Owns these entities/tables: minimal chat logs / usage metrics (if persisted)
  - Exposes these endpoints: `POST /api/ai/chat`, `GET /api/ai/history/{user}`
  - Depends on: `identity` (user context), emits usage events for analytics

Notes on grouping: the current code places `Donor` with `Gift` controllers and repositories; this suggests `catalog` should own donors as metadata tied to gifts. `AiService` is small and can remain separate or combined with `catalog` if team prefers fewer services.

## 3. Communication Between Services
- Synchronous (HTTP/REST):
  - `orders` -> `catalog` for price/availability checks (can be cached) and `identity` for user validation.
  - `catalog` -> `files` for image upload flows (or `files` returns upload URLs the client uses directly).

- Asynchronous (events):
  - `identity`: emits `UserRegistered` when new users sign up.
  - `orders`: emits `OrderCreated`, `OrderPaid`.
  - `catalog`: emits `GiftCreated`, `GiftUpdated`.
  - `lottery`: consumes `OrderPaid`/`OrderCreated` and emits `WinnerSelected`.

- Suggested event names:
  - `UserRegistered` (payload: userId, email)
  - `GiftCreated` (payload: giftId, donorId)
  - `OrderCreated` (payload: orderId, userId, items)
  - `OrderPaid` (payload: orderId, userId)
  - `WinnerSelected` (payload: lotteryId, winnerId, giftId)

## 4. Shared Concerns
- Authentication & Authorization
  - Centralize auth in `identity` using JWT tokens (current app uses `Microsoft.AspNetCore.Authentication.JwtBearer`). All services validate JWTs locally (shared public key or discovery via identity service).
  - For role checks (e.g., `Admin`) rely on roles/claims encoded in tokens.

- Shared DTOs / Contracts
  - Minimize shared DTOs; prefer small versioned contracts per-public API. Where necessary (e.g., OrderCreated event contract), define a lightweight schema in a contracts library and publish via NuGet or a shared repo.

- Database strategy
  - Recommend one database per service to maximize independent deployability and schema autonomy.
  - For `files`, use a metadata DB + object store for binary content.
  - Avoid a single shared DB for domain tables; use events to replicate necessary read-model data between services.

## 5. Migration Strategy (Strangler Fig Pattern)
- Phase 0 — Stabilize
  - Add an API gateway or reverse-proxy layer that can route to either monolith or new services.
  - Introduce an event bus (e.g., RabbitMQ, Azure Service Bus, or Kafka) and a lightweight contracts folder.

- Phase 1 — Extract `identity`
  - Why first: `identity` is a common dependency; extracting it allows issuing JWTs that new services can accept. It reduces friction for subsequent services and centralizes user data.
  - Tasks: build `identity` service from `AuthController`, `AuthService`, `UserRepository`. Migrate `Users` table and confirm token issuance.

- Phase 2 — Extract `catalog`
  - Why second: `catalog` contains core product data (`Gift`, `Category`, `Donor`) and is used by UI and orders. By extracting catalog, teams can evolve product features and image handling independently.
  - Tasks: move `GiftController`, `GiftService`, `GiftRepository`, `CategoryRepository`, `DonorRepository`, and `Mappings` related to these entities. Point controllers to `files` for uploads.

- Phase 3 — Extract `orders`
  - Why third: `orders` depends on catalog and identity. With those available as services, `orders` can own transactional behavior for purchases and ticket accounting.
  - Tasks: migrate `Order` and `OrderItem` entities, OrderRepository, OrderService and related DTOs. Emit `OrderCreated` events.

- Phase 4 — Extract `lottery`, `files`, `ai`
  - `lottery` can be event-driven and consume order events to select winners.
  - `files` (asset storage) can be extracted to centralize binary storage.
  - `ai` can be separated or remain in monolith until needed.

## 6. Summary Table
| Service | Responsibility | Tech | DB | Priority |
|---|---:|---|---|---|
| identity | Users, Auth, JWT | ASP.NET Core, EF Core | SQL (one DB) | High |
| catalog | Gifts, Categories, Donors | ASP.NET Core, EF Core | SQL (one DB) | High |
| orders | Orders & Ticketing | ASP.NET Core, EF Core | SQL (one DB) | High |
| lottery | Winner selection & draws | ASP.NET Core | SQL or NoSQL | Medium |
| files | Image/asset storage, signed URLs | ASP.NET Core + Blob store | Small metadata DB + object store | Medium |
| ai | Chat/LLM orchestration | ASP.NET Core, external APIs | Optional | Low |
