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

### `Latitude` (VO)

A `struct` that implements the full numeric interface surface — the ones `Int32` and
`Double` implement — so it is usable like a primitive while carrying its own bounds
and its own arithmetic.

| Field         | Type     | Notes                                    |
|---------------|----------|------------------------------------------|
| `Value`       | `double` | Degrees, `-90` to `90`                   |
| `Microdegrees`| `int`    | Backing value, `-900_000_000` to `900_000_000` |

### `Longitude` (VO)

Same shape as `Latitude`, with its own range and wrapping behaviour.

| Field         | Type     | Notes                                        |
|---------------|----------|----------------------------------------------|
| `Value`       | `double` | Degrees, `-180` to `180`                     |
| `Microdegrees`| `int`    | Backing value, `-1_800_000_000` to `1_800_000_000` |

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

| Field  | Type     |
|--------|----------|
| `Name` | `string` |

An opaque named thing that `Access` can be granted on. Shared owns the **type**; each
module declares its own **instances**, because the module that owns `Bin` is the only
one that should know `bin` exists:

```csharp
// BinVentory
public static class BinVentoryResources
{
    public static readonly Resource Bin      = new("bin");
    public static readonly Resource BinGroup = new("bin-group");
}
```

The dependency arrow only ever points into Shared. Humans never learns what a bin is,
and BinVentory keeps control of what may be granted on the entities it owns.

### `IResourceCatalog`

| Member | Type                     | Notes                      |
|--------|--------------------------|----------------------------|
| `All`  | `IReadOnlySet<Resource>` | Frozen after startup       |

The complete list of grantable resources, for building a role in the UI. Each module
contributes its own catalog; they are aggregated at the composition root and frozen
once.

Composed through DI rather than through a static registry populated by a private
constructor. A static registry is only complete once every module's static class has
happened to be touched, so the role-creation UI can observe a partial set depending on
which endpoint ran first; it is also not thread-safe, and it leaks between parallel
tests. Explicit registration at startup is deterministic on all three counts.

## Humans

### HumanName (VO)

| Field        | Type      | Notes    |
|--------------|-----------|----------|
| `FirstName`  | `string`  |          |
| `LastName`   | `string?` |          |
| `MiddleName` | `string?` |          |
| `FullName`   | `string`  | Computed |

### `Access` (VO)

| Field      | Type                | Notes                                  |
|------------|---------------------|----------------------------------------|
| `Actions`  | `Flags Enum : byte` | `Read`, `Write`, `Edit`, `Delete`      |
| `Resource` | `Resource`          | What the actions apply to              |

The unit of authorization. Everything else in this context exists to assign `Access`
to a caller.

Grant-only — there is no deny. Computing `User.Access` is therefore a **merge**, not a
concatenation: entries for the same `Resource` collapse into one with their `Actions`
flags OR-ed together.

**RBAC and ABAC are both intended, and either can be used alone.** An operator can run
OpenKoqis with roles only, with attribute rules only, or with both layered. Which of
those the market actually wants is unknown, so the model deliberately keeps all three
open rather than committing early.

### `Role`

| Field    | Type       | Notes |
|----------|------------|-------|
| `Id`     | `Guid`     |       |
| `Name`   | `string`   |       |
| `Access` | `Access[]` |       |

### `User`

Authentication is delegated to **Keycloak**. The domain stores no credential of any
kind — no password, no hash, no salt — so there is nothing here to verify against and
nothing to leak.

| Field             | Type        | Notes                                                        |
|-------------------|-------------|--------------------------------------------------------------|
| `Id`              | `Guid`      |                                                              |
| `SubjectId`       | `string`    | The Keycloak subject this user maps to                       |
| `Name`            | `HumanName` | Friendly display name                                        |
| `RoleId`          | `Guid?`     | Moves to Keycloak if roles are managed there                 |
| `DedicatedAccess` | `Access[]`  | Grants specific to this user                                 |
| `Access`          | `Access[]`  | **Computed** — `Role.Access` merged with `DedicatedAccess`. Not stored |
| `LocationId`      | `Guid?`     | → `Location`                                                 |
| `IsRoot`          | `bool`      | Bypasses the access check entirely                           |
| `CreatedAt`       | `DateTime`  |                                                              |
| `UpdatedAt`       | `DateTime`  |                                                              |
| `DeletedAt`       | `DateTime?` | Soft delete, 7-day retention                                 |

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

| Field               | Type        | Notes |
|---------------------|-------------|-------|
| `FillLevel`         | `FillLevel` |       |
| `GatheredAt`        | `DateTime`  |       |
| `AdditionalComment` | `string?`   |       |

### `BinVolume` (VO)

| Field   | Type    | Notes                    |
|---------|---------|--------------------------|
| `Liters`| `uint`  | Validated, max `99_999`  |

### `Bin` : `IRoutingDestination`

The source of truth for a bin's **current** state. Everything historical lives in
TimeMachine — see that section for the split.

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

`WasteCategory` is not a priority. It describes roughly what the bin is shaped for,
and `NotSpecified` is expected to be the common value for a long time.

### `BinGroup` : `IRoutingDestination`

| Field        | Type        | Notes                               |
|--------------|-------------|-------------------------------------|
| `Id`         | `Guid`      |                                     |
| `LocationId` | `Guid?`     | → `Location`                        |
| `GeoPoint`   | `GeoPoint`  | The group's single collection point |
| `FillLevel`  | `FillLevel` | Cached, computed from member bins   |

### `IRoutingDestination`

The contract TruckBrain consumes. Implemented by both `Bin` and `BinGroup`, and the
only thing the routing context knows about either of them.

| Member      | Type        |
|-------------|-------------|
| `Id`        | `Guid`      |
| `GeoPoint`  | `GeoPoint`  |
| `FillLevel` | `FillLevel` |

Which side owns this contract is undecided, and stays undecided until TruckBrain's
internals are. If routing ends up behind a wire protocol, this is a message and the
mapping belongs at the boundary rather than on the aggregate; if it stays in-process,
the interface is fine where it is.

## TimeMachine

Historical data only. BinVentory remains the source of truth for what is true *now*;
TimeMachine exists so that record of what *was* true doesn't overwhelm it.

That split is also why the context is optional. History is the expensive part of the
system — a row per bin per reading interval, most of it never read — and an operator
who doesn't want to pay for it can switch TimeMachine off and keep a fully working
system. BinVentory is core: without it OpenKoqis does not function at all.

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

| Type                                  | Status | Notes                                       |
|---------------------------------------|--------|---------------------------------------------|
| `Latitude` / `Longitude` / `GeoPoint` | ⚪      | Coordinates are loose primitives            |
| `Resource` / `IResourceCatalog`       | ⚪      |                                             |
| `Access` / `Role`                     | 🟡     | Role enum, no resource-level grants         |
| `HumanName`                           | ⚪      |                                             |
| `User`                                | 🟡     | Holds credentials; no `SubjectId`           |
| `Location`                            | ⚪      |                                             |
| `FillLevel` / `BinTelemetry`          | 🟡     | Present, not modelled as value objects      |
| `BinVolume`                           | ⚪      |                                             |
| `Bin`                                 | 🟡     | Diverges in several fields                  |
| `BinGroup`                            | ⚪      |                                             |
| `IRoutingDestination`                 | ⚪      | Ownership undecided                         |
| `BinHistory`                          | ⚪      |                                             |
| `BinCleaning`                         | 🟡     | Exists as `CleaningLog`                     |
| `Alert`                               | 🟡     | `IsResolved` bool; no `ResolvedAt`/`By`     |
| `Route`                               | ⚪      | Shape undecided                             |
