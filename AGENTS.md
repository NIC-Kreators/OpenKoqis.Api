# AGENTS.md

Orientation for working in this repository **as it currently is**.

This file describes what exists in `src/` today. It is not a specification, and it is expected to go stale between
refactors — if it contradicts the code, the file is wrong and should be updated.

The **target** state is specified in [`docs/`](docs/README.md) and is deliberately different from what is described
here: a modular monolith over Postgres with Keycloak, per-module `Domain`/`Application`/`Infrastructure`/`Api` projects.
Nothing below has been migrated to that yet. When writing new code, check
[`docs/project-structure.md`](docs/project-structure.md) before following existing patterns.

## Commands

```bash
# Run the whole stack — API, MongoDB, Mosquitto — via Aspire
aspire run

# Build the solution
dotnet build OpenKoqis.Api.slnx

# Restore
dotnet restore OpenKoqis.Api.slnx

# Format (also runs on staged files via the Husky pre-commit hook)
dotnet format OpenKoqis.Api.slnx
```

`aspire run` is the normal way to run locally — it starts MongoDB and Mosquitto as containers and injects their
connection details. There is no Docker Compose file; Aspire replaced it. Running
`dotnet run --project src/OpenKoqis.Api` directly requires Mongo and an MQTT broker already listening.

There are **no test projects** in the solution.

`TreatWarningsAsErrors=true` and `EnforceCodeStyleInBuild=true` are set on every project — warnings and style violations
fail the build. `NuGetAuditMode=direct`, and XML docs are generated with `CS1591` suppressed.

Husky.Net installs itself on restore (a `Target` in `OpenKoqis.Api.csproj`) and runs
`dotnet format` on staged `.cs` files pre-commit.

## Solution

Six projects, `net10.0`:

```
OpenKoqis.Domain              ← MongoDB.Driver only; entities and role records
OpenKoqis.Application         ← Domain; CQRS handlers, ErrorOr, Mediator, MongoDB.Driver
OpenKoqis.Infrastructure      ← Domain + Application; JWT and password hashing only
OpenKoqis.Api                 ← all of the above; controllers, MQTT, Program.cs
OpenKoqis.Api.ServiceDefaults ← Aspire defaults (OTel, health, resilience, discovery)
OpenKoqis.Api.AppHost         ← Aspire orchestration
```

## Architecture

Clean Architecture layering with **CQRS** in the Application layer. Requests go:

```
Controller → ISender.Send(Command/Query) → Handler → IMongoCollection<T>
```

There is **no repository abstraction**. Handlers take `IMongoDatabase` by constructor injection and call
`database.GetCollection<T>("Bins")` themselves, with the collection name as a string literal in the handler.

Results use **ErrorOr**: handlers return `ErrorOr<T>`, and controllers unwrap with
`.Match(Ok, Problem)`.

### Domain (`OpenKoqis.Domain`)

- `Shared/Entity<TId>` — identity equality by `Id`.
- `Shared/ValueObject` — structural equality via `GetEqualityComponents()`.
- `Shared/IUseCase<TInput,TOutput>` / `IEventCase<TInput>` — unused by the CQRS code.
- `Models/IEntity` — marker interface; a leftover of the removed generic repository.
- `Models/UserRole` — abstract `record` hierarchy `AdminRole > SalesManagerRole >
  GuestRole`, each a singleton (`Instance`), compared via `HasPermissionsOf()` and parsed from a JWT claim with
  `UserRole.Parse(string)`.
- `Models/` — `User`, `Bin`, `BinTelemetry`, `Alert`, `CleaningLog`, `ShiftLog`.

The project references `MongoDB.Driver` — entities carry Mongo attributes and
`ObjectId`-derived string ids.

### Application (`OpenKoqis.Application`)

Organized by feature, not by layer:

```
Features/{Alerts,Bins,CleaningLogs,ShiftLogs,Users}/
    Commands/     one file per command: the record + its handler
    Queries/      one file per query: the record + its handler
    Errors/       static class of ErrorOr Error factories, e.g. BinErrors.NotFound(id)
```

Each file holds both the message and its handler:

```csharp
public record GetBinByIdQuery(string Id) : IRequest<ErrorOr<Bin>>;

public class GetBinByIdQueryHandler(IMongoDatabase database, ILogger<...> logger)
    : IRequestHandler<GetBinByIdQuery, ErrorOr<Bin>>
{
    private readonly IMongoCollection<Bin> _collection = database.GetCollection<Bin>("Bins");

    public async ValueTask<ErrorOr<Bin>> Handle(GetBinByIdQuery request, CancellationToken ct)
        => await _collection.Find(b => b.Id == request.Id).FirstOrDefaultAsync(ct)
           ?? BinErrors.NotFound(request.Id);
}
```

Mediator is the **source-generated** `martinothamar/Mediator` (`Mediator.Abstractions`
in Application, `Mediator.SourceGenerator` in Api) — handlers return `ValueTask`, not
`Task`. It is not MediatR; don't reach for MediatR APIs.

`Services/` retains only `IJwtService` and `IPasswordHasher`. The former per-entity service interfaces are gone,
replaced by the handlers above.

`FluentValidation.DependencyInjectionExtensions` is referenced but no validators exist and no validation behavior is
registered.

### Infrastructure (`OpenKoqis.Infrastructure`)

Only two implementations remain:

- `JwtService` — 15-minute HS256 access token plus refresh token. Refresh tokens live in an in-memory
  `ConcurrentDictionary` and are lost on restart.
- `BCryptPasswordHasher` — BCrypt.Net-Core wrapper.

### Api (`OpenKoqis.Api`)

- `Controllers/ApiController` — abstract base carrying `[ApiController]`,
  `[Route("api/[controller]")]`, and the `Problem(List<Error>)` overload that maps
  `ErrorType` to a status code. All controllers derive from it.
- Controllers inject `ISender` and do little beyond dispatching — except
  `BinsController`, which filters query results in-memory after `GetAllBinsQuery` and orchestrates several commands per
  request (telemetry update, history append, then conditional alert creation at `FillLevel >= 90` and on smoke).
- `Mqtt/MqttClientService` — a `BackgroundService` subscribing to topics from
  `Mqtt:Topics`; parses the bin id out of the topic as `topic.Split('/')[1]`.
- `Attributes/AuthorizeRoleAttribute` — `[AuthorizeRole(typeof(AdminRole))]` resolves by reflection to the policy
  `MinimumRole_{RoleName}`.
- `Extensions/AuthorizationExtensions` — registers `MinimumRole_Admin`,
  `MinimumRole_SalesManager`, `MinimumRole_Guest`; `ValidateToken()` checks the role claim against the hierarchy.
- `Extensions/DbExtension.AddDbContext()` — binds `MongoOptions` from the
  `MongoSettings` section, registers `IMongoClient`, `IMongoDatabase`, and
  `MongoDbService`.
- `Services/MongoDbService` — exposes `Users` and `Bins` collections; largely bypassed, since handlers resolve
  `IMongoDatabase` themselves.
- Scalar API docs at `/docs`.
- Serilog via `UseSerilogRequestLogging()`, configured from `appsettings.json`.

### Aspire

`AppHost.cs` provisions:

- **MongoDB** `openkoqis-mongo` with database `openkoqis`, a persistent data volume, and credentials from the
  `mongo-username` / `mongo-password` parameters. It sets
  `GLIBC_TUNABLES=glibc.pthread.rseq=1` to work around a MongoDB incompatibility with Linux kernels ≥ 6.19 — remove once
  the official image moves to a 7.1+ base.
- **Mosquitto** `openkoqis-mqtt` on 1883, with `config/mosquitto/{config,logs,data}`
  bind-mounted from the AppHost content root.
- The API project, referencing both.

`ServiceDefaults` adds OpenTelemetry (ASP.NET Core, HttpClient, runtime instrumentation), health checks at `/health`,
standard resilience handlers, and service discovery. The OTLP exporter activates only when
`OTEL_EXPORTER_OTLP_ENDPOINT` is set.

## Configuration

Config merges `appsettings.json`, environment variables, and a `.env` file in the working directory (DotNetEnv). Copy
`src/OpenKoqis.Api/.env.example` →
`src/OpenKoqis.Api/.env`:

| Variable                          | Purpose                           |
|-----------------------------------|-----------------------------------|
| `MONGO_CONNECTION_STRING`         | MongoDB connection string         |
| `MQTT_HOST` / `MQTT_PORT`         | Mosquitto broker address          |
| `MQTT_CLIENT_ID`                  | MQTT client identifier            |
| `MQTT_ALLOW_ANONYMOUS`            | Skip broker credentials if `true` |
| `MQTT_USERNAME` / `MQTT_PASSWORD` | Required when not anonymous       |

`appsettings.json` holds `MongoSettings` (database and collection names), `Jwt`
(`Key`, `Issuer`, `Audience`), `Mqtt`, Serilog, and a Kestrel endpoint on `0.0.0.0:8080`.

## Known gaps

Verified against the current tree. These are traps, not style opinions — check here before concluding something is
broken on your machine.

- **Authentication is never wired up.** `Program.cs` calls
  `AddAuthorizationSecPolicies()` but there is no `AddAuthentication()`, no JWT bearer configuration, and no
  `UseAuthentication()` / `UseAuthorization()` in the pipeline. The `[AuthorizeRole]` attributes and `MinimumRole_*`
  policies therefore do not enforce anything, and every endpoint is open.
- **The MQTT service never starts.** `MqttClientService` is a `BackgroundService` but is registered with `AddScoped`,
  not `AddHostedService`. Nothing resolves it, so no telemetry is ever consumed.
- **Aspire's Mongo connection is not consumed.** Aspire injects
  `ConnectionStrings__openkoqis`, while `DbExtension` reads `MONGO_CONNECTION_STRING`
  and throws if it is absent. The API falls back to whatever is in `.env` rather than the container Aspire started.
  `MongoSettings:DatabaseName` is also `OpenKoqisDatabase`
  while the Aspire database is `openkoqis`.
- **A JWT signing key is committed** in `appsettings.json`
  (`Jwt:Key = "MySuperSecretKey12345678901234567890"`). Treat it as compromised; it needs to move to a parameter or user
  secret.
- **`src/OpenKoqis.Application.Features.Bins.Errors/BinErrors.cs`** is an orphan — no
  `.csproj`, not in the solution, so it never compiles. It has diverged from the live
  `Features/Bins/Errors/BinErrors.cs` (it carries an extra
  `InvalidTelemetryTimestamp()` and a different namespace). Delete it, or move the missing error into the real file.
- **The root `.env.example` is stale** — `MONGODB_*`, `GRAFANA_*`, `SEQ_API_KEY` are left over from the removed Compose
  stack. Likewise `appsettings.json` points Serilog at `http://open-koqis.otel-collector:4318`, a host that no longer
  exists.
- **Collection names are string literals** repeated across handlers, while
  `MongoOptions` defines the same names as configuration. The options are effectively unused.
- `IEntity`, `IUseCase`, and `IEventCase` are dead — nothing implements or consumes them since the generic repository
  was removed.
