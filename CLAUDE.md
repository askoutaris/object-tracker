# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ObjectTracker is a .NET library that tracks objects and identifies differences after changes are made. It's distributed as a NuGet package targeting netstandard2.0.

## Solution Structure

The solution contains three projects:

- **ObjectTracker**: Main library project (netstandard2.0) - the core tracking functionality
- **Tests**: xUnit test project (net8.0)
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

The library uses a fluent API pattern to configure object tracking. Core components:

### Tracker<TType, TDiff>

The main tracking class that stores a snapshot of an object and tracks specified properties/collections.

- **Entry point**: `Tracker<TType, TDiff>.CreateNew(source)` creates a new tracker instance
- **Fluent API**: Methods return `this` to enable method chaining
- **Generic design**: `TType` is the object being tracked, `TDiff` is the difference output type

### Tracking Mechanisms

The tracker uses nested classes (defined as partial class members in separate files):

1. **TrackedValue<TValue>** (TrackerValue.cs): Tracks individual property values
   - Compares source value against target value using `.Equals()`
   - Invokes `differenceFactory` when values differ or when null state changes

2. **TrackCollection<TItem>** (TrackerCollection.cs): Tracks collections of items
   - Matches items between source and target collections using a matching predicate
   - Recursively tracks individual items using nested `Tracker<TItem, TDiff>` instances
   - Detects added items (in target but not in source) via `addedFactory`
   - Detects removed items (in source but not in target) via `removedFactory`

### Key Design Patterns

- **Partial classes**: The main `Tracker<TType, TDiff>` class is split across three files (Tracker.cs, TrackerValue.cs, TrackerCollection.cs)
- **Nested types**: Internal tracking implementations (`ITrackedValue`, `TrackedValue<TValue>`, `TrackCollection<TItem>`) are nested within the main tracker class
- **Factory pattern**: Difference objects are created via user-provided factory functions, giving full control over the diff representation

## Code Style

- Uses C# latest language version with implicit usings enabled
- Primary constructors are disabled (see .editorconfig: IDE0290 severity = none)
- Nullable reference types enabled
- Collection expressions preferred (e.g., `[]` instead of `new List<>()`)
