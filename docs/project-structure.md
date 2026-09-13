# Project Structure

Solution layout, project boundaries, and the dependency rules between them.

> [!warning] This is not documentation of the current codebase.
> It describes the **desired** structure. `src/` currently holds a flat, non-modular
> four-project solution (`OpenKoqis.Domain` / `.Application` / `.Infrastructure` /
> `.Api`); everything below is the target to migrate toward. See
> [README.md](README.md) for how these documents relate to the code.

## Shape

A **modular monolith**. One deployable host, with each Bounded Context from
[domain-model.md](domain-model.md) as a self-contained module of four projects.

The module boundary is what makes Geography and TimeMachine optional. Disabling a context is not a configuration flag
threaded through the code — it is a module whose registration is not called. Nothing else has to know.

## Layout

```
src/
  Shared/
    OpenKoqis.Shared.Kernel/
    OpenKoqis.Shared.Api/

  Modules/
    Humans/
      OpenKoqis.Humans.Domain/
      OpenKoqis.Humans.Application/
      OpenKoqis.Humans.Infrastructure/
      OpenKoqis.Humans.Api/
    Geography/       (same four)
    BinVentory/      (same four)
    TimeMachine/     (same four)
    TruckBrain/      (same four)

  Host/
    OpenKoqis.Host/
    OpenKoqis.AppHost/
    OpenKoqis.ServiceDefaults/
```

## The four projects of a module

Taking BinVentory as the example.

### `OpenKoqis.BinVentory.Domain`

Entities, value objects, enums, domain events, and the invariants that protect them.
`Bin`, `BinGroup`, `FillLevel`, `BinTelemetry`, `BinVolume`.

References **only** `Shared.Kernel`. No EF, no ASP.NET, no DI container, no other module. If this project needs a
package reference to do its job, that's a signal the logic belongs a layer up.

### `OpenKoqis.BinVentory.Application`

Use cases and the **ports** they need — repository interfaces, clock, the contracts for anything outside the module.
Commands, queries, handlers, validation, and the authorization rules expressed in terms of `Resource` and `Access`.

References its own `Domain` and `Shared.Kernel`. This is also where the module's resources are declared:

```csharp
public sealed class BinVentoryResources : IResourceCatalog
{
    public static readonly Resource Bin      = new("bin");
    public static readonly Resource BinGroup = new("bin-group");

    public IReadOnlySet<ResourceDescriptor> All { get; } = /* ... */;
}
```

### `OpenKoqis.BinVentory.Infrastructure`

The adapters that implement the ports: the module's `DbContext`, EF configurations, migrations, and any external client.
Owns its **own schema** in the shared Postgres instance — `binventory.*` — so the module's tables are as separable as
its code.

References `Application`, `Domain`, `Shared.Kernel`. Exposes one registration extension,
`AddBinVentoryInfrastructure(IServiceCollection, IConfiguration)`.

### `OpenKoqis.BinVentory.Api`

The module's HTTP surface — endpoints, request/response contracts, mapping — plus the single public registration
extension that wires the whole module up:

```csharp
public static IServiceCollection AddBinVentory(
    this IServiceCollection services, IConfiguration config)
{
    services.AddSingleton<IResourceCatalog, BinVentoryResources>();
    services.AddBinVentoryInfrastructure(config);
    // handlers, validators, ...
    return services;
}
```

References `Application`, `Infrastructure`, `Shared.Kernel`, `Shared.Api`. This is the only project outside the module
anyone is allowed to reference.

## Shared

### `OpenKoqis.Shared.Kernel`

Cross-cutting domain primitives with no owner: `GeoPoint`, `Latitude`, `Longitude`,
`Resource`, `IResourceCatalog`, `ResourceRegistry`, `Access`, entity and value object base types, `Result`.

Shared owns these **types**; it never owns module **instances**. `Resource` lives here, `new Resource("bin")` lives in
BinVentory. The dependency arrow points into Shared and never back out — that's the property that lets Humans do
authorization on bins without knowing bins exist.

Keep it small. A type belongs here only when two modules genuinely need it; one module needing it means it belongs to
that module.

### `OpenKoqis.Shared.Api`

Web-layer plumbing every module's `Api` project repeats otherwise: problem details, exception handling, the
authorization handler that evaluates `Access`, pagination, endpoint conventions.

Split out `Shared.Application` and `Shared.Infrastructure` only once there is real content for them. Empty layers
created for symmetry are a cost with no return.

## Host

### `OpenKoqis.Host`

The composition root, and the only project that references module `Api` projects:

```csharp
builder.Services
    .AddHumans(config)
    .AddBinVentory(config)
    .AddTruckBrain(config);

if (config.GetValue<bool>("Modules:Geography"))   builder.Services.AddGeography(config);
if (config.GetValue<bool>("Modules:TimeMachine")) builder.Services.AddTimeMachine(config);
```

`ResourceRegistry` is built here, from every `IResourceCatalog` the registered modules contributed — which is why a
disabled module's resources are correctly absent from the role editor rather than grantable and dead.

Holds `Program.cs`, configuration, and nothing else. No business logic ever lands in this project.

### `OpenKoqis.AppHost`

Aspire orchestration: Postgres, Keycloak, the TimeMachine store, the RouteEngine process, and the API itself, wired
together with their connection strings and dependencies. Development and deployment topology — no domain code.

### `OpenKoqis.ServiceDefaults`

The Aspire defaults every service shares: OpenTelemetry, health checks, resilience handlers, service discovery.

## Dependency rules

1. A module's `Domain` references `Shared.Kernel` and nothing else.
2. A module never references another module's `Domain`, `Application`, or
   `Infrastructure`. Cross-module communication goes through the other module's
   `Api`-level contracts, or through domain events.
3. Only `OpenKoqis.Host` references module `Api` projects.
4. Nothing references `Host` or `AppHost`.
5. `Shared.Kernel` references nothing inside the solution.

Rules 1–3 are the ones that decay quietly, and a code review will not reliably catch a single new `ProjectReference`.
Enforce them with an architecture test (NetArchTest or ArchUnitNET) that fails the build. The boundaries are the entire
point of this layout; untested boundaries are just a folder convention.

## Implementation status

| Item                        | Status | Notes                                        |
|-----------------------------|--------|----------------------------------------------|
| Modular layout              | ⚪     | `src/` is a flat four-project solution       |
| `Shared.Kernel`             | ⚪     |                                              |
| `Shared.Api`                | ⚪     |                                              |
| Humans module               | ⚪     |                                              |
| Geography module            | ⚪     |                                              |
| BinVentory module           | ⚪     |                                              |
| TimeMachine module          | ⚪     |                                              |
| TruckBrain module           | ⚪     |                                              |
| `OpenKoqis.Host`            | 🟡     | `OpenKoqis.Api` is host + controllers + MQTT |
| `OpenKoqis.AppHost`         | 🟡     | `OpenKoqis.Api.AppHost`; Mongo + Mosquitto   |
| `OpenKoqis.ServiceDefaults` | 🟢     | `OpenKoqis.Api.ServiceDefaults`              |
| Architecture tests          | ⚪     | No test projects in the solution             |
