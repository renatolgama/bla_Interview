# BLA Tasks — .NET Technical Interview Exercise

[![Backend CI](https://github.com/renatolgama/bla_Interview/actions/workflows/backend.yml/badge.svg)](https://github.com/renatolgama/bla_Interview/actions/workflows/backend.yml)
[![Frontend CI](https://github.com/renatolgama/bla_Interview/actions/workflows/frontend.yml/badge.svg)](https://github.com/renatolgama/bla_Interview/actions/workflows/frontend.yml)

A full-stack task management application built for the Ballast Lane .NET Technical
Interview Exercise: ASP.NET Core Web API with Clean Architecture and TDD, SQL Server,
JWT authentication, and a React + TypeScript frontend.

## User story

> As a registered user, I want to log in and manage my tasks — create, list, edit,
> change status, and delete — so I can organize my work. Each task has a title,
> description, status, and due date, and only its owner can see or change it.

## Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API, C# |
| Data | Entity Framework Core, SQL Server 2022 (Docker) |
| Auth | Custom JWT (HS256) + BCrypt password hashing |
| Tests | xUnit, NSubstitute, FluentAssertions, WebApplicationFactory |
| Frontend | React 19, Vite, TypeScript |

## Architecture

Clean Architecture — dependencies point inward only:

```
Bla.Api  ──►  Bla.Infrastructure  ──►  Bla.Application  ──►  Bla.Domain
(controllers,  (EF Core, repositories,   (business rules,      (entities,
 JWT config,    JWT/BCrypt services,      validation, DTOs,     enums — zero
 DI, Swagger)   migrations, seeding)      interfaces)           dependencies)
```

- **Domain** — `User`, `TaskItem`, `TaskItemStatus`. No references to anything.
- **Application** — all business rules and validation; defines the interfaces
  (`ITaskRepository`, `IUserRepository`, `IPasswordHasher`, `ITokenService`, `IClock`)
  that outer layers implement. No EF Core, no ASP.NET types.
- **Infrastructure** — EF Core `DbContext`, repository implementations, BCrypt hasher,
  JWT token service, migrations, and demo data seeding. Task list reads go through a
  read-through cache decorator (60s TTL, invalidated per user on every write) that
  wraps the EF repository via DI — the Application layer never knows caching exists.
- **Api** — thin controllers, JWT bearer authentication, global error handling
  (RFC 7807 ProblemDetails), Swagger. Composition root: DI is wired here.

```
BLA/
├── src/
│   ├── Bla.Domain/
│   ├── Bla.Application/
│   ├── Bla.Infrastructure/
│   └── Bla.Api/
├── tests/
│   ├── Bla.Application.Tests/       # business rules (mocked dependencies)
│   ├── Bla.Infrastructure.Tests/    # repositories over SQLite in-memory
│   └── Bla.Api.Tests/               # endpoints via WebApplicationFactory
├── frontend/                        # React + Vite + TypeScript
├── docs/
│   └── genai-process.md             # GenAI deliverable: prompt, output, validation
└── docker-compose.yml               # SQL Server 2022
```

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Start SQL Server

```bash
docker compose up -d
```

### 2. Run the API

```bash
dotnet run --project src/Bla.Api
```

On startup (Development), the API applies EF Core migrations automatically and seeds
demo data. Swagger UI: <http://localhost:5000/swagger>.

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

App: <http://localhost:5173>.

### Demo credentials (seeded)

| Email | Password |
|---|---|
| `demo@ballastlane.com` | `Demo123!` |

The demo user comes with five sample tasks. You can also register a new account.

## Running the tests

```bash
dotnet test
```

84 tests across three suites — Application (45, mocked dependencies),
Infrastructure (23, EF Core over SQLite in-memory) and Api (16, full HTTP
pipeline via WebApplicationFactory). Tests never require Docker. The suites
were written test-first: each layer has a `test(...) (red)` commit that
predates its `feat(...) (green)` commit.

## API endpoints

### Auth — `/api/auth`

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | Create a user account |
| POST | `/api/auth/login` | Anonymous | Authenticate, returns a JWT |
| GET | `/api/auth/me` | **Bearer** | Current user's profile |
| GET | `/api/auth/health` | Anonymous | Liveness/public endpoint |

### Tasks — `/api/tasks` (all require a Bearer token; scoped to the token's user)

| Method | Route | Description |
|---|---|---|
| GET | `/api/tasks?status=Todo&page=1&pageSize=10` | List my tasks — paged (`items`, `totalCount`, `totalPages`), optional status filter |
| GET | `/api/tasks/{id}` | Get one of my tasks |
| POST | `/api/tasks` | Create a task (201 + Location) |
| PUT | `/api/tasks/{id}` | Update a task |
| DELETE | `/api/tasks/{id}` | Delete a task (204) |

Error responses follow RFC 7807 (`application/problem+json`): 400 for validation
errors, 401 for missing/invalid/expired tokens, 404 for unknown ids — including tasks
owned by someone else (the API never confirms that another user's resource exists).

## Business rules (Application layer, each pinned by unit tests)

- Title: required, ≤ 200 chars. Description: optional, ≤ 2000 chars.
- Due date cannot be in the past on creation.
- Registration: valid and unique email; password ≥ 8 chars with letters and digits;
  BCrypt (work factor 12) for storage.
- Login failures return a generic "invalid credentials" — the API never reveals
  whether an email is registered.
- Ownership is enforced inside the service layer (defense in depth), not only in
  database queries.

## GenAI process

This project was built with Claude Code using a gated, review-driven workflow
(brainstorm → approved plan → TDD phases → human review of every diff). The full
deliverable — the engineered prompt, representative output, and how AI suggestions
were validated and corrected — is in [docs/genai-process.md](docs/genai-process.md).
