# ObjectTracker

A lightweight .NET library for tracking objects and detecting changes. ObjectTracker uses a builder pattern to create reusable trackers that capture snapshots of your objects and compare them to detect what has changed.

## Features

- **Reusable Trackers** - Configure once, track multiple objects for optimal performance
- **Simple Fluent API** - Chain configuration methods for intuitive setup
- **Property Tracking** - Track individual properties and detect changes
- **Collection Tracking** - Track collections with item-level change detection (additions, removals, modifications)
- **Nested Tracking** - Recursively track properties of collection items
- **Custom Difference Types** - Define your own difference representations
- **Contextual Differences** - Access the target object in difference factories for rich context
- **Type-Safe** - Fully generic implementation with compile-time type safety
- **Optimized Performance** - Efficient collection matching algorithms with O(n) complexity

## Installation

```bash
dotnet add package ObjectTracker
```

Or via NuGet Package Manager:

```
Install-Package ObjectTracker
```

## Quick Start

```csharp
using ObjectTracker.Builders;

// 1. Build a reusable tracker (configure once)
var trackerBuilder = new TrackerBuilder<Person, string>()
    .TrackProperty(
        selector: p => p.Name,
        differenceFactory: (person, oldValue, newValue) =>
            $"Name changed from {oldValue} to {newValue}")
    .TrackProperty(
        selector: p => p.Age,
        differenceFactory: (person, oldValue, newValue) =>
            $"Age changed from {oldValue} to {newValue}");

var tracker = trackerBuilder.Build();

// 2. Track an object (captures snapshot)
var person = new Person { Name = "John", Age = 30 };
var trackedPerson = tracker.Track(person);

// 3. Modify the object
person.Name = "Jane";
person.Age = 31;

// 4. Get the differences
var differences = trackedPerson.GetDifferences();

foreach (var diff in differences)
{
    Console.WriteLine(diff);
}
// Output:
// Name changed from John to Jane
// Age changed from 30 to 31
```

## How It Works

1. **Build a Tracker** - Use `TrackerBuilder` to configure what properties and collections to track. Build it once to create a reusable `Tracker` instance.
2. **Track Objects** - Call `tracker.Track(object)` to capture a snapshot. Returns a `TrackedObject` with the frozen state.
3. **Make Changes** - Modify your object as needed.
4. **Compare** - Call `trackedObject.GetDifferences()` to detect changes, or `trackedObject.Compare(anotherObject)` to compare against a different object.

The tracker is reusable - configure once and track thousands of objects efficiently. Each tracked object maintains its own immutable snapshot.

## Why Use TrackerBuilder?

**Performance Optimization**: The builder pattern separates configuration from state capture. Build your tracker once and reuse it:

```csharp
var tracker = new TrackerBuilder<Person, Difference>()
    .TrackProperty(p => p.Name, (p, old, newVal) => new Difference("Name", old, newVal))
    .Build();

// Track multiple objects efficiently - no need to rebuild the configuration
var tracked1 = tracker.Track(person1);
var tracked2 = tracker.Track(person2);
var tracked3 = tracker.Track(person3);
```

## Tracking Properties

Track any property using a selector and a factory function. The factory receives the **target object** for contextual information:

```csharp
var tracker = new TrackerBuilder<Person, Difference>()
    .TrackProperty(
        selector: p => p.Email,
        differenceFactory: (person, oldEmail, newEmail) =>
            new Difference($"Email changed for {person.Id}", oldEmail, newEmail))
    .Build();

var trackedPerson = tracker.Track(person);
person.Email = "newemail@example.com";
var differences = trackedPerson.GetDifferences();
```

You can track computed values:

```csharp
var tracker = new TrackerBuilder<Person, Difference>()
    .TrackProperty(
        selector: p => p.Name.Length,
        differenceFactory: (person, oldLength, newLength) =>
            new Difference("NameLength", oldLength, newLength))
    .Build();
```

## Tracking Collections

Track collections of items with add/remove/modify detection:

```csharp
var tracker = new TrackerBuilder<ShoppingCart, string>()
    .TrackCollection(
        itemsSelector: c => c.Items,
        matchingPredicate: (item1, item2) => item1.Id == item2.Id,
        addedFactory: (src, tgt, item) => $"Added: {item.Name}",
        removedFactory: (src, tgt, item) => $"Removed: {item.Name}",
        configureItemTracker: itemBuilder => itemBuilder)
    .Build();

var cart = new ShoppingCart
{
    Items = new List<Product>
    {
        new Product { Id = 1, Name = "Apple", Price = 1.50m },
        new Product { Id = 2, Name = "Banana", Price = 0.80m }
    }
};

var trackedCart = tracker.Track(cart);

// Modify the collection
cart.Items.Add(new Product { Id = 3, Name = "Orange", Price = 1.20m });
cart.Items.RemoveAt(0); // Remove Apple

var changes = trackedCart.GetDifferences();
// Changes will contain:
// - "Removed: Apple"
// - "Added: Orange"
```

### Nested Collection Tracking

Track properties of collection items using `configureItemTracker`:

```csharp
var tracker = new TrackerBuilder<ShoppingCart, string>()
    .TrackCollection(
        itemsSelector: c => c.Items,
        matchingPredicate: (item1, item2) => item1.Id == item2.Id,
        addedFactory: (src, tgt, item) => $"Added: {item.Name}",
        removedFactory: (src, tgt, item) => $"Removed: {item.Name}",
        configureItemTracker: itemBuilder => itemBuilder
            // Track price changes for matched items
            .TrackProperty(
                selector: p => p.Price,
                differenceFactory: (product, oldPrice, newPrice) =>
                    $"Product {product.Id}: Price changed from {oldPrice:C} to {newPrice:C}")
            // Track name changes for matched items
            .TrackProperty(
                selector: p => p.Name,
                differenceFactory: (product, oldName, newName) =>
                    $"Product {product.Id}: Name changed from '{oldName}' to '{newName}'"))
    .Build();
```

## Custom Difference Types

Define your own types to represent differences:

```csharp
public interface IDifference { }

public class PropertyChanged : IDifference
{
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public PropertyChanged(string propertyName, object? oldValue, object? newValue)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

public class ItemAdded : IDifference
{
    public object Item { get; }

    public ItemAdded(object item)
    {
        Item = item;
    }
}

// Use in tracker
var tracker = new TrackerBuilder<Person, IDifference>()
    .TrackProperty(
        selector: p => p.Name,
        differenceFactory: (person, old, newVal) =>
            new PropertyChanged("Name", old, newVal))
    .Build();
```

## API Reference

### TrackerBuilder\<TType, TDiff>

Builder for configuring reusable trackers.

#### Constructor
```csharp
new TrackerBuilder<TType, TDiff>()
```
Creates a new tracker builder.

#### TrackProperty\<TValue>(selector, differenceFactory)
Configures tracking for a property or computed value.

**Parameters:**
- `selector` - Function to select the value to track from the object
- `differenceFactory` - Function to create a difference. Receives `(targetObject, oldValue, newValue)`

**Returns:** `TrackerBuilder<TType, TDiff>` (for chaining)

#### TrackCollection\<TItem>(itemsSelector, matchingPredicate, addedFactory, removedFactory, configureItemTracker)
Configures tracking for a collection of items.

**Parameters:**
- `itemsSelector` - Function to select the collection from the object
- `matchingPredicate` - Function to match items between source and target collections
- `addedFactory` (optional) - Function to create a difference for added items. Receives `(source, target, addedItem)`
- `removedFactory` (optional) - Function to create a difference for removed items. Receives `(source, target, removedItem)`
- `configureItemTracker` - Function to configure tracking for matched items. Receives a `TrackerBuilder<TItem, TDiff>`

**Returns:** `TrackerBuilder<TType, TDiff>` (for chaining)

#### Build()
Creates a reusable tracker from the configuration.

**Returns:** `ITracker<TType, TDiff>`

### ITracker\<TType, TDiff>

Reusable tracker that can snapshot multiple objects.

#### Track(source)
Captures a snapshot of the source object.

**Parameters:**
- `source` - The object to track

**Returns:** `ITrackedObject<TType, TDiff>` - An object containing the snapshot and comparison methods

### ITrackedObject\<TType, TDiff>

A tracked object with a captured snapshot.

#### Properties
- `Source` - The original object that was tracked

#### GetDifferences()
Compares the source object against its original snapshot (detects if the source was mutated).

**Returns:** `IReadOnlyCollection<TDiff>` - Collection of detected differences

#### Compare(target)
Compares the original snapshot against a different target object.

**Parameters:**
- `target` - The object to compare against

**Returns:** `IReadOnlyCollection<TDiff>` - Collection of detected differences

## Use Cases

- **Audit Logging** - Track changes to domain objects for compliance
- **Undo/Redo Systems** - Identify what changed to implement undo functionality
- **Data Synchronization** - Detect differences before syncing to databases or APIs
- **Change Notifications** - Generate user-friendly change descriptions with full context
- **State Management** - Track application state changes in UI frameworks
- **Form Validation** - Detect which fields have been modified
- **API Request Optimization** - Only send changed fields in PATCH requests
- **Batch Processing** - Configure once, efficiently track thousands of objects

## Performance

ObjectTracker is optimized for both single-object and high-volume tracking scenarios:

- **Reusable Configuration**: Build tracker once, reuse for unlimited objects
- **Optimized Collection Matching**: O(n) algorithm with HashSet-based lookups
- **Single Enumeration**: Target collections materialized once to avoid repeated iteration
- **Early Exit**: Matching stops at first match for better average-case performance
- **Memory Efficient**: Two-tier design (configuration vs state) minimizes per-object overhead

## Requirements

- .NET Standard 2.0 or higher
- Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+, .NET 6+, .NET 7+, .NET 8+

## License

MIT License - see LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Repository

[https://github.com/askoutaris/object-tracker](https://github.com/askoutaris/object-tracker)
