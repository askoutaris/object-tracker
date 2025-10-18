# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ObjectTracker is a .NET library that tracks objects and identifies differences after changes are made. It uses a builder pattern to configure reusable trackers that can snapshot multiple objects efficiently. Distributed as a NuGet package targeting netstandard2.0.

## Solution Structure

The solution contains three projects:

- **ObjectTracker**: Main library project (netstandard2.0) - the core tracking functionality
- **Tests**: xUnit test project (net8.0) - comprehensive unit tests
- **Workbench**: Console application for manual testing/experimentation (net8.0)

## Build Commands

```bash
# Build entire solution
dotnet build

# Build specific configuration
dotnet build -c Release
dotnet build -c Debug

# Build specific project
dotnet build ObjectTracker/ObjectTracker.csproj
```

## Test Commands

```bash
# Run all tests
dotnet test

# Run tests with code coverage (Windows)
cd Tests
./coverage.bat
# This will run tests, generate coverage reports, and open the HTML report in browser

# Run tests manually with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Package Management

```bash
# Create NuGet package (from ObjectTracker directory)
dotnet pack -c Release --include-symbols

# Or use the provided script
cd ObjectTracker
./dotnet-pack.cmd
```

## Architecture

The library uses a **builder pattern** to separate configuration from state capture, enabling tracker reuse for performance optimization.

### Core Workflow

```csharp
// 1. Build a reusable tracker (configuration only, no state)
var tracker = new TrackerBuilder<Person, Difference>()
    .TrackProperty(p => p.Name, (p, old, newVal) => new Difference("Name", old, newVal))
    .TrackCollection(...)
    .Build();

// 2. Track objects (captures snapshot)
var trackedPerson = tracker.Track(person);

// 3. Compare against snapshot
var differences = trackedPerson.GetDifferences();
// or compare against different target
var differences = trackedPerson.Compare(anotherPerson);
```

### Directory Structure

```
ObjectTracker/
├── Builders/
│   └── TrackerBuilder.cs          # Fluent API to configure tracking
├── Trackers/
│   ├── Tracker.cs                  # Reusable tracker (configuration template)
│   └── ItemTrackers/
│       ├── @IItemTracker.cs        # Interface for tracker components
│       ├── PropertyTracker.cs      # Configures property tracking
│       └── CollectionTracker.cs    # Configures collection tracking
└── TrackedObjects/
    ├── TrackedObject.cs            # Captured state snapshot with comparison logic
    └── TrackedItems/
        ├── @ITrackedItem.cs        # Interface for tracked items
        ├── TrackedProperty.cs      # Property snapshot + comparison
        └── TrackedCollection.cs    # Collection snapshot + comparison
```

### Key Components

#### 1. TrackerBuilder<TType, TDiff>
- **Purpose**: Fluent API to configure what to track
- **Methods**:
  - `TrackProperty<TValue>(selector, differenceFactory)` - Configure property tracking
  - `TrackCollection<TItem>(itemsSelector, matchingPredicate, factories, configureItemTracker)` - Configure collection tracking
  - `Build()` - Create a reusable `Tracker<TType, TDiff>`
- **Location**: `Builders/TrackerBuilder.cs`

#### 2. Tracker<TType, TDiff>
- **Purpose**: Reusable configuration template (no state, can track multiple objects)
- **Contains**: Collection of `IItemTracker` instances (configuration only)
- **Method**: `Track(source)` - Creates a `TrackedObject<TType, TDiff>` with captured snapshot
- **Location**: `Trackers/Tracker.cs`

#### 3. ItemTrackers (Configuration Layer)
Reusable configuration objects that create tracked items:

- **PropertyTracker<TType, TDiff, TValue>**: Holds selector and difference factory
  - `GetTrackedItem(source)` creates `TrackedProperty` with value snapshot

- **CollectionTracker<TType, TDiff, TItem>**: Holds collection selectors, predicates, and nested tracker
  - `GetTrackedItem(source)` creates `TrackedCollection` with item snapshots

**Location**: `Trackers/ItemTrackers/`

#### 4. TrackedObject<TType, TDiff>
- **Purpose**: Immutable snapshot of an object's tracked state
- **Contains**: Collection of `ITrackedItem` instances (each holding captured values)
- **Properties**: `Source` - the original object
- **Methods**:
  - `GetDifferences()` - Compare source against itself (detects mutations)
  - `Compare(target)` - Compare source against different target object
- **Location**: `TrackedObjects/TrackedObject.cs`

#### 5. TrackedItems (State Layer)
Captured snapshots with comparison logic:

- **TrackedProperty<TType, TDiff, TValue>**:
  - Stores `_sourceValue` (snapshot at track time)
  - Holds references to `selector` and `differenceFactory` for comparison
  - `GetDifferences(target)` compares snapshot vs target value using `.Equals()`

- **TrackedCollection<TType, TDiff, TItem>**:
  - Stores collection of `ITrackedObject<TItem, TDiff>` (nested snapshots)
  - Holds references to selectors, predicates, and factories
  - `GetDifferences(target)` performs optimized O(n) matching with HashSet tracking
  - Detects additions, removals, and nested item changes

**Location**: `TrackedObjects/TrackedItems/`

### Key Design Patterns

#### Builder Pattern
Separates configuration from execution. The builder creates reusable `Tracker` instances that can snapshot multiple objects without rebuilding the configuration tree.

**Performance Benefit**: Configure once, reuse for thousands of objects
```csharp
var tracker = builder.Build(); // Configure once
var tracked1 = tracker.Track(obj1); // Reuse
var tracked2 = tracker.Track(obj2); // Reuse
```

#### Two-Tier Abstraction
- **ItemTrackers**: Stateless configuration templates (reusable)
- **TrackedItems**: Stateful snapshots (per-object instances)

This separation enables memory-efficient tracking at scale.

#### Factory Pattern
Difference objects are created via user-provided factory functions. The factory receives the **entire target object** for contextual difference messages:

```csharp
differenceFactory: (person, oldValue, newValue) =>
    new Difference($"Name changed for person {person.Id}")
```

#### Recursive Tracking
Collections use nested `Tracker` instances for items, enabling deep hierarchical tracking:

```csharp
.TrackCollection(
    itemsSelector: p => p.Addresses,
    matchingPredicate: (a1, a2) => a1.Id == a2.Id,
    configureItemTracker: itemBuilder => itemBuilder
        .TrackProperty(a => a.City, ...) // Recursive tracking
)
```

### Comparison Algorithm Optimization

**TrackedCollection** uses an optimized O(n) matching algorithm:
1. Materialize target collection to array (single enumeration)
2. Use `HashSet<int>` to track matched indices (O(1) lookups)
3. Early exit on first match (better average-case performance)

## Code Style

- Uses C# latest language version with implicit usings enabled
- Primary constructors are disabled (see .editorconfig: IDE0290 severity = none)
- Nullable reference types enabled
- Collection expressions preferred (e.g., `[]` instead of `new List<>()`)
- Internal classes use interface-based design for testability
- `ref` parameters used for delegate passing (though delegates are reference types, so this has minimal impact)

## Testing

All tests use xUnit with standard `Assert` methods (no FluentAssertions). Tests are organized by component:
- **TrackerBuilderTests.cs**: Builder fluent API and configuration
- **TrackerTests.cs**: Tracker reusability and TrackedObject comparison
- **CollectionTrackerTests.cs**: Collection tracking with nested scenarios

Run tests: `dotnet test`
