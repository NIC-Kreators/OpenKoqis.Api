# Data Model

Field-level reference for every entity and value object in the domain.

> [!warning] This is not documentation of the current codebase.
> It describes the **desired** state of the data model, not what is implemented today.
> The project is in active development and `src/` lags behind — when the two
> disagree, the code is the thing that's wrong. See [README.md](README.md) for the
> convention, and the status table at the bottom of this document for how far along
> each piece is.

For context boundaries, aggregate roles, and how these types relate to one another,
see [domain-model.md](domain-model.md). This document is deliberately just the fields.

## Conventions

- All `Guid` identifiers are **UUID v7** — time-ordered, so they sort by creation and index without page splits.
- `CreatedAt` / `UpdatedAt` are present on entities that are edited in place, and omitted on records that are written
  once and never changed.
- `DeletedAt` marks a soft delete. The row is retained for **7 days**, then purged.
- `?` marks a nullable field. Every reference to `Location` is nullable because the Geography context is optional.
- `(VO)` marks a value object: no identity of its own, defined entirely by its attributes, and stored inline in whatever
  owns it.

## Shared

The shared kernel (`OpenKoqis.Shared.Kernel`). Every module may depend on it; it depends on nothing but `ErrorOr`.

### Building blocks

- `Entity<TId>` — base for entities. Equality is by runtime type and `Id`; nothing else is compared.
- `ValueObject` — base for value objects. Equality is by runtime type and the components returned from
  `GetEqualityComponents()`, in order.

### Validation rules

Format rules shared by value objects across modules. Each is a static `IsValid(ReadOnlySpan<char>)`; the tables below
refer to them by name.

| Rule               | Accepts                                                                                                       |
|--------------------|---------------------------------------------------------------------------------------------------------------|
| `SystemNameRules`  | kebab-case: lowercase ASCII letters, digits and single hyphens; starts with a letter; default max length `16` |
| `NameRules`        | Unicode letters only; default max length `16`                                                                 |
| `EmailRules`       | A pragmatic subset of RFC 5321, max `254` characters, TLD of `2`+ letters                                     |
| `PhoneNumberRules` | E.164: `+` followed by `8`–`15` digits, first digit not `0`                                                   |

### `Latitude` (VO)

A ` readonly record struct` that implements the full numeric interface surface — the ones `Int32` and
`Double` implement — so it is usable like a primitive while carrying its own bounds and its own arithmetic.

| Field          | Type     | Notes                                          |
|----------------|----------|------------------------------------------------|
| `Value`        | `double` | Degrees, `-90` to `90`                         |
| `Microdegrees` | `int`    | Backing value, `-900_000_000` to `900_000_000` |

### `Longitude` (VO)

Same shape as `Latitude`, with its own range and wrapping behaviour.

| Field          | Type     | Notes                                              |
|----------------|----------|----------------------------------------------------|
| `Value`        | `double` | Degrees, `-180` to `180`                           |
| `Microdegrees` | `int`    | Backing value, `-1_800_000_000` to `1_800_000_000` |

Arithmetic stays inside the valid range rather than overflowing:

```csharp
Longitude l1 = 170;
Longitude l2 = 50;

Longitude total = l1 + l2; // -140
```

### `GeoPoint` (VO)

| Field       | Type        |
|-------------|-------------|
| `Latitude`  | `Latitude`  |
| `Longitude` | `Longitude` |

### `Resource` (VO)

| Field                | Type                   | Notes                       |
|----------------------|------------------------|-----------------------------|
| `Name`               | `string`               | `SystemNameRules`, max `32` |
| `AllowedPermissions` | `IReadOnlySet<string>` | `SystemNameRules`, `1`+     |

An opaque named thing that `Access` can be granted on. Shared owns the **type**; each module declares its own
**instances**, because the module that owns `Bin` is the only one that should know `bin` exists:

```csharp
// BinVentory
public static class BinVentoryResources
{
    public static readonly Resource Bin = new("bin", CommonPermissions.All);
    public static readonly Resource BinGroup = new("bin-group", CommonPermissions.All);
    public static readonly Resource BinInstall = new("bin-install", ["create", "approve", "reject"]);
}

// Injects into the DI
public class BinVentoryResourceCatalog : IResourceCatalog
{
    public string ModuleName => "BinVentory";
    public IReadOnlySet<Resource> All { get; } = FrozenSet.Create(Bin, BinGroup, BinInstall);
}
```

The dependency arrow only ever points into Shared. Humans never learns what a bin is, and BinVentory keeps control of
what may be granted on the entities it owns.

### `IResourceCatalog`

| Member       | Type                     | Notes                |
|--------------|--------------------------|----------------------|
| `ModuleName` | `string`                 |                      |
| `All`        | `IReadOnlySet<Resource>` | Frozen after startup |

The complete list of grantable resources, for building a role in the UI. Each module contributes its own catalog; they
are aggregated at the composition root and frozen once.

Composed through DI rather than through a static registry populated by a private constructor. A static registry is only
complete once every module's static class has happened to be touched, so the role-creation UI can observe a partial set
depending on which endpoint ran first; it is also not thread-safe, and it leaks between parallel tests. Explicit
registration at startup is deterministic on all three counts.

### `Access` (VO)

| Field         | Type                   | Notes                                            |
|---------------|------------------------|--------------------------------------------------|
| `Resource`    | `string`               | Lowercased, trimmed; `SystemNameRules`, max `32` |
| `Permissions` | `IReadOnlySet<string>` | Lowercased, trimmed; `SystemNameRules`           |

The unit of authorization. Everything in Humans exists to assign `Access` to a caller. It lives in Shared because every
module checks it, while only Humans assigns it.

Written as `resource:permission,permission` (e.g. `bin:read,write`) and parsed with `Access.Parse`, which reports every
invalid name as a separate error.

Grant-only — there is no deny. Computing a user's effective access is therefore a **merge**, not a concatenation:
`Access.Merge` collapses entries for the same `Resource` into one, uniting their `Permissions`.

**RBAC and ABAC are both intended, and either can be used alone.** An operator can run OpenKoqis with roles only, with
attribute rules only, or with both layered. Which of those the market actually wants is unknown, so the model
deliberately keeps all three open rather than committing early.

## Humans

### `HumanName` (VO)

| Field        | Type      | Notes                                      |
|--------------|-----------|--------------------------------------------|
| `FirstName`  | `string`  | `NameRules`                                |
| `LastName`   | `string?` | `NameRules`                                |
| `MiddleName` | `string?` | `NameRules`                                |
| `FullName`   | `string`  | Computed: first, last, middle; compared on |

Parsed from a single string of one to three space-separated words, taken in the order first, last, middle.

### `Login` (VO)

| Field   | Type     | Notes                                   |
|---------|----------|-----------------------------------------|
| `Value` | `string` | Normalized form, as stored and compared |

What a user signs in with. Abstract, with exactly three kinds; `Login.Parse` picks one from the raw input:

| Kind          | Chosen when        | `Value`                                                  |
|---------------|--------------------|----------------------------------------------------------|
| `Email`       | input contains `@` | Trimmed, lowercased; `EmailRules`                        |
| `PhoneNumber` | input starts `+`   | E.164, separators `' -.()'` stripped; `PhoneNumberRules` |
| `Username`    | otherwise          | Trimmed; `SystemNameRules`, max `32`                     |

The kind is part of equality, so a username and an email with the same text are never equal.

### `Role`

| Field       | Type                   | Notes                |
|-------------|------------------------|----------------------|
| `Id`        | `Guid`                 |                      |
| `Name`      | `string`               | Trimmed; `NameRules` |
| `Accesses`  | `IReadOnlySet<Access>` | `1`+ at creation     |
| `CreatedBy` | `Guid`                 | → `User`             |
| `CreatedAt` | `DateTime`             |                      |
| `UpdatedAt` | `DateTime`             |                      |

A named, reusable bundle of `Access`. Accesses are granted and revoked as whole entries: revoking removes an entry only
when both its resource and its full permission set match.

### `User`

Authentication is delegated to **Keycloak**. The domain stores no credential of any kind — no password, no hash, no
salt — so there is nothing here to verify against and nothing to leak.

| Field             | Type                   | Notes                                                         |
|-------------------|------------------------|---------------------------------------------------------------|
| `Id`              | `Guid`                 |                                                               |
| `Login`           | `Login`                | What the user signs in with                                   |
| `Email`           | `Email?`               | Contact email; defaults to `Login` when it is an `Email`      |
| `PhoneNumber`     | `PhoneNumber?`         | Contact phone; defaults to `Login` when it is a `PhoneNumber` |
| `IdentityId`      | `string`               | The Keycloak subject this user maps to; `1`–`2048` chars      |
| `Name`            | `HumanName`            | Friendly display name                                         |
| `RoleId`          | `Guid?`                | → `Role`. Moves to Keycloak if roles are managed there        |
| `DedicatedAccess` | `IReadOnlySet<Access>` | Grants specific to this user; `1`+ for a non-root user        |
| `LocationId`      | `Guid?`                | → `Location`                                                  |
| `IsRoot`          | `bool`                 | Bypasses the access check entirely                            |
| `CreatedAt`       | `DateTime`             |                                                               |
| `UpdatedAt`       | `DateTime`             |                                                               |
| `DeletedAt`       | `DateTime?`            | Soft delete, 7-day retention                                  |

Effective access is `DedicatedAccess` merged with the role's `Accesses` (see `Access`). Resolving it takes the user's
current `Role` and rejects any other role with `User.RoleMismatch`.

There is exactly one **root** user. It has no role and no dedicated access, and both are immutable. Uniqueness is
enforced by persistence, which reports a second root as `User.RootAlreadyExists`.

## Geography

### `Location`

| Field              | Type         | Notes                                           |
|--------------------|--------------|-------------------------------------------------|
| `Id`               | `Guid`       |                                                 |
| `Name`             | `string`     |                                                 |
| `GeoPoints`        | `GeoPoint[]` | One point, or **3+** points to describe an area |
| `ParentLocationId` | `Guid?`      | Self-referencing hierarchy                      |
| `CreatedAt`        | `DateTime`   |                                                 |
| `UpdatedAt`        | `DateTime`   |                                                 |

## BinVentory

### `FillLevel` (VO)

| Field   | Type   | Notes                        |
|---------|--------|------------------------------|
| `Value` | `byte` | Percent, validated `0`–`100` |

### `BinTelemetry` (VO)

Bin can not support some kind of metrics. Maybe it's better to make something like `BinSettings` VO.

| Field               | Type           | Notes                                        |
|---------------------|----------------|----------------------------------------------|
| `FillLevel`         | `FillLevel?`   |                                              |
| `Temperature`       | `Temperature?` | Maybe it's VO will have short with Unit enum |
| `IsSmokeDetectorOn` | `bool?`        |                                              |
| `GatheredAt`        | `DateTime`     |                                              |
| `AdditionalComment` | `string?`      |                                              |

### `BinVolume` (VO)

| Field    | Type   | Notes                   |
|----------|--------|-------------------------|
| `Liters` | `uint` | Validated, max `99_999` |

### `Bin` : `IRoutingDestination`

The source of truth for a bin's **current** state. Everything historical lives in TimeMachine — see that section for the
split.

| Field              | Type            | Notes                                                   |
|--------------------|-----------------|---------------------------------------------------------|
| `Id`               | `Guid`          |                                                         |
| `LocationId`       | `Guid?`         | → `Location`                                            |
| `GeoPoint`         | `GeoPoint`      | Always set — a bin is addressable without a `Location`  |
| `BinGroupId`       | `Guid?`         | → `BinGroup`                                            |
| `State`            | `BinState`      | `Working` \| `Cleaning` \| `NotAvailable` \| `Disabled` |
| `BinType`          | `BinType`       | `Dumpster` \| `CityBin`                                 |
| `WasteCategory`    | `WasteCategory` | `NotSpecified` \| `MetalAndGlass` \| `...`              |
| `CurrentTelemetry` | `BinTelemetry?` | Latest reading                                          |
| `ActiveAlert`      | `Alert?`        | Currently unresolved alert, if any                      |
| `Volume`           | `BinVolume?`    |                                                         |
| `LastCleanedAt`    | `DateTime?`     | When it was cleaned last time                           |
| `CreatedAt`        | `DateTime`      |                                                         |
| `UpdatedAt`        | `DateTime`      |                                                         |
| `DeletedAt`        | `DateTime?`     | Soft delete, 7-day retention                            |

`WasteCategory` is not a priority. It describes roughly what the bin is shaped for, and `NotSpecified` is expected to be
the common value for a long time.

### `BinGroup` : `IRoutingDestination`

| Field        | Type        | Notes                               |
|--------------|-------------|-------------------------------------|
| `Id`         | `Guid`      |                                     |
| `LocationId` | `Guid?`     | → `Location`                        |
| `GeoPoint`   | `GeoPoint`  | The group's single collection point |
| `FillLevel`  | `FillLevel` | Cached, computed from member bins   |

### `IRoutingDestination`

The contract TruckBrain consumes. Implemented by both `Bin` and `BinGroup`, and the only thing the routing context knows
about either of them.

| Member      | Type        |
|-------------|-------------|
| `Id`        | `Guid`      |
| `GeoPoint`  | `GeoPoint`  |
| `FillLevel` | `FillLevel` |

Which side owns this contract is undecided, and stays undecided until TruckBrain's internals are. If routing ends up
behind a wire protocol, this is a message and the mapping belongs at the boundary rather than on the aggregate; if it
stays in-process, the interface is fine where it is.

## TimeMachine

Historical data only. BinVentory remains the source of truth for what is true *now*; TimeMachine exists so that record
of what *was* true doesn't overwhelm it.

That split is also why the context is optional. History is the expensive part of the system — a row per bin per reading
interval, most of it never read — and an operator who doesn't want to pay for it can switch TimeMachine off and keep a
fully working system. BinVentory is core: without it OpenKoqis does not function at all.

### `BinHistory`

| Field   | Type           | Notes                |
|---------|----------------|----------------------|
| `Id`    | `Guid`         |                      |
| `BinId` | `Guid`         | → `Bin`              |
| `State` | `BinTelemetry` | One archived reading |

The highest-volume table in the system: one row per bin per reading interval.

### `BinCleaning`

| Field       | Type       | Notes   |
|-------------|------------|---------|
| `Id`        | `Guid`     |         |
| `BinId`     | `Guid`     | → `Bin` |
| `CleanedAt` | `DateTime` |         |

Written once, never updated.

### `Alert`

| Field        | Type        | Notes                                  |
|--------------|-------------|----------------------------------------|
| `Id`         | `Guid`      |                                        |
| `BinId`      | `Guid`      | → `Bin`                                |
| `Type`       | `AlertType` | See below                              |
| `CreatedAt`  | `DateTime`  |                                        |
| `ResolvedAt` | `DateTime?` | Null while the alert is open           |
| `ResolvedBy` | `Guid?`     | → `User`. Null while the alert is open |

`AlertType`: `AlmostFull` \| `Disconnected` \| `SmokeDetected` \| `LifeDetected` \|
`RealShitDetected`.

## TruckBrain

### `Route`

| Field          | Type                    | Notes   |
|----------------|-------------------------|---------|
| `Destinations` | `IRoutingDestination[]` | Ordered |

Shape not yet decided beyond the ordered destination list — no id, status, assignment, or timestamps are settled. See
the open questions in
[domain-model.md](domain-model.md).

## Implementation status

Markers: ⚪ planned · 🟡 partial, diverges from this document · 🟢 matches this document.

| Type                                  | Status | Notes                                   |
|---------------------------------------|--------|-----------------------------------------|
| `Latitude` / `Longitude` / `GeoPoint` | ⚪     | Coordinates are loose primitives        |
| `Entity` / `ValueObject` / rules      | 🟢     |                                         |
| `Resource` / `IResourceCatalog`       | 🟡     | No `ModuleName`; no module catalogs yet |
| `Access`                              | 🟢     |                                         |
| `HumanName` / `Login`                 | 🟢     |                                         |
| `Role`                                | 🟢     | Domain only; not persisted              |
| `User`                                | 🟡     | Domain only; not persisted, no Keycloak |
| `Location`                            | ⚪     |                                         |
| `FillLevel` / `BinTelemetry`          | 🟡     | Present, not modelled as value objects  |
| `BinVolume`                           | ⚪     |                                         |
| `Bin`                                 | 🟡     | Diverges in several fields              |
| `BinGroup`                            | ⚪     |                                         |
| `IRoutingDestination`                 | ⚪     | Ownership undecided                     |
| `BinHistory`                          | ⚪     |                                         |
| `BinCleaning`                         | 🟡     | Exists as `CleaningLog`                 |
| `Alert`                               | 🟡     | `IsResolved` bool; no `ResolvedAt`/`By` |
| `Route`                               | ⚪     | Shape undecided                         |
