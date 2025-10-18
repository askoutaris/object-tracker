# ObjectTracker

A lightweight .NET library for tracking objects and detecting changes. ObjectTracker takes a snapshot of your object's state and compares it against modified versions to identify what has changed.

## Features

- **Simple Fluent API** - Chain configuration methods for intuitive setup
- **Property Tracking** - Track individual properties and detect changes
- **Collection Tracking** - Track collections with item-level change detection (additions, removals, modifications)
- **Nested Tracking** - Recursively track properties of collection items
- **Custom Difference Types** - Define your own difference representations
- **Type-Safe** - Fully generic implementation with compile-time type safety
- **Optimized Performance** - Efficient collection matching algorithms

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
using ObjectTracker;

// Create an object to track
var person = new Person
{
    Name = "John",
    Age = 30
};

// Create a tracker and configure what to track
var tracker = Tracker<Person, string>.CreateNew(person)
    .Track(
        selector: p => p.Name,
        differenceFactory: (oldValue, newValue) =>
            $"Name changed from {oldValue} to {newValue}")
    .Track(
        selector: p => p.Age,
        differenceFactory: (oldValue, newValue) =>
            $"Age changed from {oldValue} to {newValue}");

// Modify the object
person.Name = "Jane";
person.Age = 31;

// Get the differences
string[] differences = tracker.Compare(person);

foreach (var diff in differences)
{
    Console.WriteLine(diff);
}
// Output:
// Name changed from John to Jane
// Age changed from 30 to 31
```

## How It Works

1. **Create a Tracker** - Pass your object to `Tracker<TType, TDiff>.CreateNew()`. This captures a snapshot of its current state.
2. **Configure Tracking** - Use `.Track()` for properties and `.TrackItems()` for collections.
3. **Make Changes** - Modify your object as needed.
4. **Compare** - Call `tracker.Compare(object)` to get an array of differences.

The tracker always compares against the original snapshot, so you can call `Compare()` multiple times with different states.

## Tracking Properties

Track any property using a selector and a factory function to create differences:

```csharp
var tracker = Tracker<Person, Difference>.CreateNew(person)
    .Track(
        selector: p => p.Email,
        differenceFactory: (oldEmail, newEmail) =>
            new Difference("Email", oldEmail, newEmail));
```

You can track computed values:

```csharp
tracker.Track(
    selector: p => p.Name.Length,
    differenceFactory: (oldLength, newLength) =>
        new Difference("NameLength", oldLength, newLength));
```

## Tracking Collections

Track collections of items with add/remove/modify detection:

```csharp
var cart = new ShoppingCart
{
    Items = new List<Product>
    {
        new Product { Id = 1, Name = "Apple", Price = 1.50m },
        new Product { Id = 2, Name = "Banana", Price = 0.80m }
    }
};

var tracker = Tracker<ShoppingCart, string>.CreateNew(cart)
    .TrackItems(
        itemsSelector: c => c.Items,
        matchingPredicate: (item1, item2) => item1.Id == item2.Id,
        addedFactory: (src, tgt, item) => $"Added: {item.Name}",
        removedFactory: (src, tgt, item) => $"Removed: {item.Name}");

// Modify the collection
cart.Items.Add(new Product { Id = 3, Name = "Orange", Price = 1.20m });
cart.Items.RemoveAt(0); // Remove Apple

var changes = tracker.Compare(cart);
// Changes will contain:
// - "Removed: Apple"
// - "Added: Orange"
```

### Nested Collection Tracking

Track properties of collection items using `configureTracker`:

```csharp
var tracker = Tracker<ShoppingCart, string>.CreateNew(cart)
    .TrackItems(
        itemsSelector: c => c.Items,
        matchingPredicate: (item1, item2) => item1.Id == item2.Id,
        addedFactory: (src, tgt, item) => $"Added: {item.Name}",
        removedFactory: (src, tgt, item) => $"Removed: {item.Name}",
        configureTracker: itemTracker =>
        {
            // Track price changes for matched items
            itemTracker.Track(
                selector: p => p.Price,
                differenceFactory: (oldPrice, newPrice) =>
                    $"Product {itemTracker.Source.Id}: Price changed from {oldPrice:C} to {newPrice:C}");

            // Track name changes for matched items
            itemTracker.Track(
                selector: p => p.Name,
                differenceFactory: (oldName, newName) =>
                    $"Product {itemTracker.Source.Id}: Name changed from '{oldName}' to '{newName}'");
        });
```

## Custom Difference Types

Define your own types to represent differences:

```csharp
public interface IDifference { }

public class PropertyChanged : IDifference
{
    public string PropertyName { get; }
    public object OldValue { get; }
    public object NewValue { get; }

    public PropertyChanged(string propertyName, object oldValue, object newValue)
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
var tracker = Tracker<Person, IDifference>.CreateNew(person)
    .Track(
        selector: p => p.Name,
        differenceFactory: (old, newVal) =>
            new PropertyChanged("Name", old, newVal));
```

## API Reference

### Tracker\<TType, TDiff>

#### CreateNew(TType source)
Creates a new tracker instance and captures a snapshot of the source object.

**Parameters:**
- `source` - The object to track

**Returns:** `ITracker<TType, TDiff>`

#### Track\<TValue>(selector, differenceFactory)
Tracks a property or computed value.

**Parameters:**
- `selector` - Function to select the value to track
- `differenceFactory` - Function to create a difference when values don't match

**Returns:** `Tracker<TType, TDiff>` (for chaining)

#### TrackItems\<TItem>(itemsSelector, matchingPredicate, addedFactory, removedFactory, configureTracker)
Tracks a collection of items.

**Parameters:**
- `itemsSelector` - Function to select the collection
- `matchingPredicate` - Function to match items between source and target collections
- `addedFactory` (optional) - Function to create a difference for added items
- `removedFactory` (optional) - Function to create a difference for removed items
- `configureTracker` (optional) - Action to configure tracking for matched items

**Returns:** `Tracker<TType, TDiff>` (for chaining)

#### Compare(TType target)
Compares the target against the original snapshot.

**Parameters:**
- `target` - The object to compare

**Returns:** `TDiff[]` - Array of differences

## Use Cases

- **Audit Logging** - Track changes to domain objects for compliance
- **Undo/Redo Systems** - Identify what changed to implement undo functionality
- **Data Synchronization** - Detect differences before syncing to databases or APIs
- **Change Notifications** - Generate user-friendly change descriptions
- **State Management** - Track application state changes in UI frameworks
- **Form Validation** - Detect which fields have been modified
- **API Request Optimization** - Only send changed fields in PATCH requests

## Performance

ObjectTracker uses optimized algorithms for collection matching:
- Target collections are materialized once to avoid repeated enumeration
- HashSet-based tracking ensures O(1) lookups for matched items
- Early exit on match for better average-case performance

## Requirements

- .NET Standard 2.0 or higher
- Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+

## License

MIT License - see LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Repository

[https://github.com/askoutaris/object-tracker](https://github.com/askoutaris/object-tracker)
