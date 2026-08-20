# GenAI Process — Task Management REST API

> Deliverable for the "Generative AI tools" section of the Ballast Lane .NET Technical
> Interview Exercise. It covers: (1) the prompt I would use to generate the API,
> (2) representative output code, and (3) how I validated, corrected, and improved
> the AI's suggestions — including edge cases, authentication, and validation.

**Tool used:** Claude Code (Anthropic's agentic CLI, running inside VS Code).
I chose an agentic tool over autocomplete-style assistants (Copilot inline) because the
task is *architectural*: it spans a solution structure, multiple layers, tests, and
infrastructure. Agentic tools can plan, create files, run the test suite, and iterate on
failures — which fits a TDD workflow. The trade-off is that they produce large diffs, so
the review discipline (section 4) matters even more.

**Important context:** this repository itself was built with Claude Code. So this document
is not hypothetical — the prompts below are the real ones driving this project, and the
"corrections" section lists real interventions I made on real AI output during this build.

---

## 1. My workflow (before the prompt itself)

A single "generate me an API" mega-prompt produces plausible code with hidden flaws. I
split the work into gated phases, each with a human review checkpoint:

```
Requirements → Brainstorm (AI asks, I decide) → Written plan (I approve)
→ TDD implementation in small phases → I review every diff → Commit per phase
```

Key prompt-engineering principles applied:

- **Make the AI ask first.** I start in a brainstorming mode where the AI must ask
  clarifying questions one at a time before writing anything. This surfaced decisions I
  own: SQL Server vs SQLite, custom JWT vs ASP.NET Identity, React vs Angular.
- **Plan as a contract.** The AI writes a plan file (architecture, endpoints, business
  rules, test strategy). I approve or edit it *before* code exists. Reviewing a plan is
  cheap; reviewing 3,000 lines of surprise code is not.
- **Tests define "done".** TDD forces the AI to state expected behavior (the test) before
  the implementation, which makes hallucinated behavior visible immediately.
- **Small phases, small diffs.** One layer at a time, one commit per phase. I never
  review more than I can actually read.

---

## 2. The prompt

This is the implementation prompt I use (after requirements are agreed). It is written to
be tool-agnostic — it works in Claude Code, Cursor, or Windsurf:

```text
You are a senior .NET engineer. Build a RESTful API for a task management system.

## Context
- .NET 10, ASP.NET Core Web API, C#, Entity Framework Core, SQL Server.
- Clean Architecture with four projects:
  - Domain: entities and enums only. Zero dependencies.
  - Application: business rules, validation, service interfaces, DTOs.
    Depends only on Domain. No EF Core, no ASP.NET types here.
  - Infrastructure: EF Core DbContext, repositories, JWT token service,
    BCrypt password hashing, data seeding. Implements Application interfaces.
  - Api: controllers, JWT bearer authentication, global error handling
    middleware (RFC 7807 ProblemDetails), Swagger. Composition root for DI.

## Functional requirements
- Task entity: Id (Guid), Title, Description, Status (Todo | InProgress | Done),
  DueDate, UserId (owner), CreatedAt, UpdatedAt.
- User entity: Id (Guid), Email (unique), Name, PasswordHash, CreatedAt.
- Auth endpoints: POST /api/auth/register, POST /api/auth/login (returns JWT),
  GET /api/auth/me (authorized), GET /api/auth/health (anonymous).
- Task endpoints (all require a valid JWT, all scoped to the token's user):
  GET /api/tasks?status=, GET /api/tasks/{id}, POST /api/tasks,
  PUT /api/tasks/{id}, DELETE /api/tasks/{id}.
- Correct status codes: 200/201/204 on success; 400 validation; 401 no/invalid
  token; 404 not found. Return 404 (not 403) when a user requests another
  user's task — do not leak that the resource exists.

## Business rules (enforce in Application layer, not in controllers)
- Title: required, max 200 chars. Description: optional, max 2000 chars.
- DueDate: must not be in the past on creation.
- Registration: valid email format, unique email, password min 8 chars with at
  least one letter and one digit. Hash with BCrypt (work factor 12).
- Login failure: return a single generic "invalid credentials" error — never
  reveal whether the email exists.
- Ownership: every task operation must verify the task belongs to the
  authenticated user, inside the service (defense in depth), not only via query
  filters in the controller.

## Non-functional requirements
- TDD: for each service, write failing xUnit tests first (NSubstitute for
  mocks), then implement until green. Also add controller tests via
  WebApplicationFactory using SQLite in-memory instead of SQL Server.
- Use DateTime.UtcNow everywhere (never DateTime.Now). Serialize enums as
  strings. Async/await end to end with CancellationToken support.
- No secrets in source: JWT key and connection string come from configuration.
- Zero build warnings. Nullable reference types enabled.
- Seed data on startup (dev only): one demo user and five sample tasks.

## Process
Work in this order: Domain → Application (tests first) → Infrastructure → Api.
After each layer, run `dotnet build` and `dotnet test` and show me the results
before moving on. If a test fails, fix the code, not the test — unless the test
itself contradicts a requirement above, in which case stop and ask me.
```

Why this prompt is shaped this way:

| Technique | Where | Why |
|---|---|---|
| Role + quality bar | "senior .NET engineer", "zero warnings" | Anchors output style and rigor |
| Explicit architecture | project-by-project dependency rules | Prevents the #1 AI failure: EF Core types leaking into business logic |
| Security stated as rules, not vibes | BCrypt factor, generic 401, 404-not-403, no secrets | AI defaults are insecure-by-omission; each rule closes a real hole |
| Edge cases enumerated up front | past DueDate, duplicate email, foreign task | If you don't list them, the AI ships the happy path |
| Process constraints | tests first, run after each layer, "fix the code, not the test" | Stops the classic failure mode of AI weakening tests to make them pass |
| An escape hatch | "stop and ask me" | Gives the AI a correct action when requirements conflict, instead of guessing |

---

## 3. Representative output

The full output is this repository (`src/` and `tests/`). A representative sample — the
ownership check at the heart of the task service, exactly as generated and reviewed:

```csharp
// src/Bla.Application/Services/TaskService.cs (excerpt)
public async Task<TaskResponse> UpdateAsync(
    Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken ct)
{
    var task = await _taskRepository.GetByIdAsync(taskId, ct);

    // 404 for both "does not exist" and "not yours": never reveal
    // other users' resource ids.
    if (task is null || task.UserId != userId)
        throw new NotFoundException($"Task '{taskId}' was not found.");

    ValidateTitle(request.Title);
    task.Title = request.Title.Trim();
    task.Description = request.Description?.Trim();
    task.Status = request.Status;
    task.DueDate = request.DueDate;
    task.UpdatedAt = _clock.UtcNow;

    await _taskRepository.UpdateAsync(task, ct);
    return TaskResponse.FromEntity(task);
}
```

And the test that was written *before* it, pinning the security behavior:

```csharp
// tests/Bla.Application.Tests/TaskServiceTests.cs (excerpt)
[Fact]
public async Task UpdateAsync_WhenTaskBelongsToAnotherUser_ThrowsNotFound()
{
    var task = TaskBuilder.For(ownerId).Build();
    _taskRepository.GetByIdAsync(task.Id, default).Returns(task);

    var act = () => _sut.UpdateAsync(attackerId, task.Id, ValidRequest(), default);

    await act.Should().ThrowAsync<NotFoundException>(); // 404, not 403
}
```

---

## 4. How I validated the AI's suggestions

I treat AI output as a pull request from a fast, well-read, occasionally overconfident
junior: never merged unread.

1. **Read every diff.** Small phases keep diffs reviewable. I read the code before
   running it, looking specifically at boundaries (what depends on what) and at anything
   security-adjacent.
2. **Tests are the contract.** Because tests were written first and I reviewed them, the
   implementation has to satisfy behavior *I* approved. `dotnet test` runs after every
   phase; a green suite over reviewed tests is much stronger evidence than a green suite
   over AI-invented tests.
3. **Verify against official docs, not vibes.** AI confidently uses APIs that don't
   exist or are outdated (see corrections below). Anything unfamiliar — a package name, a
   method overload, a middleware registration order — gets checked against Microsoft
   docs or the package's release notes before I accept it.
4. **Security checklist pass** on the auth/token/hashing code specifically: work factor,
   token expiry and signing algorithm, what claims go into the JWT, error messages that
   could leak account existence, secrets in config vs source.
5. **Run the real thing.** Swagger + the React frontend against the seeded database:
   register, login, CRUD, expired-token behavior, another-user's-task behavior. Unit
   tests don't catch DI misconfiguration or middleware ordering — running does.

## 5. Corrections and improvements I made to AI output

Real interventions from this build (updated as the build progresses):

| AI produced | Problem | My correction |
|---|---|---|
| `DateTime.Now` in entity timestamps (early draft) | Server-local time breaks sorting/consistency across timezones | `DateTime.UtcNow` behind an `IClock` abstraction so time is testable |
| Enum `TaskStatus` for the entity status | Collides with `System.Threading.Tasks.TaskStatus`, forcing qualification everywhere | Renamed to `TaskItemStatus`; entity named `TaskItem` for the same reason |
| Ownership filtering only in the EF query (`Where(t => t.UserId == userId)`) | Correct result, but the rule lives in Infrastructure — untestable as a business rule and easy to forget in a new query | Explicit ownership check in the Application service (defense in depth), pinned by unit test |
| `FluentAssertions` latest (v8) suggested for tests | v8 moved to a paid commercial license | Pinned v7.x (Apache 2.0) — an informed dependency decision, not just "latest" |
| Happy-path-only controller tests | Missed the cases that actually break in demos: 401 without token, 400 on invalid body, 404 on foreign task | Required a test per failure mode before accepting the controller as done |

## 6. Edge cases, authentication, and validation — how they're handled

- **Validation** lives in the Application layer (not just data annotations on DTOs), so
  the rules are enforced no matter who calls the service, and each rule has a unit test:
  empty/whitespace title, title over max length, past due date, malformed email,
  weak password, duplicate email.
- **Authentication:** custom JWT (HS256, 60-min expiry, user id + email claims) issued on
  login; BCrypt (work factor 12) for password storage; `[Authorize]` by default on task
  endpoints with explicit `[AllowAnonymous]` only on register/login/health. Generic
  "invalid credentials" on login failure.
- **Authorization edge case:** requesting, updating, or deleting another user's task
  returns **404**, not 403 — the API never confirms that a resource id exists for
  someone else.
- **Token edge cases:** missing, malformed, and expired tokens all produce 401 via the
  JWT bearer middleware; covered by API tests.
- **Data edge cases:** unknown ids return 404; status filter with an invalid value
  returns 400; concurrent-friendly `UpdatedAt` audit field on every mutation.
