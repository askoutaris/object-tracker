using ObjectTracker.Builders;

namespace ObjectTracker.Tests;

public class TrackerBuilderTests
{
	private record Person(string Name, int Age, string? Email);
	private record Difference(string PropertyName, object? OldValue, object? NewValue);

	[Fact]
	public void Build_ShouldCreateTracker()
	{
		// Arrange
		var builder = new TrackerBuilder<Person, Difference>();

		// Act
		var tracker = builder.Build();

		// Assert
		Assert.NotNull(tracker);
	}

	[Fact]
	public void TrackProperty_ShouldReturnBuilderForFluentAPI()
	{
		// Arrange
		var builder = new TrackerBuilder<Person, Difference>();

		// Act
		var result = builder.TrackProperty(
			selector: p => p.Name,
			differenceFactory: (p, old, newVal) => new Difference("Name", old, newVal));

		// Assert
		Assert.Same(builder, result);
	}

	[Fact]
	public void TrackProperty_CanChainMultipleCalls()
	{
		// Arrange
		var builder = new TrackerBuilder<Person, Difference>();

		// Act
		var result = builder
			.TrackProperty(p => p.Name, (p, old, newVal) => new Difference("Name", old, newVal))
			.TrackProperty(p => p.Age, (p, old, newVal) => new Difference("Age", old, newVal))
			.TrackProperty(p => p.Email, (p, old, newVal) => new Difference("Email", old, newVal));

		// Assert
		Assert.NotNull(result);
	}

	[Fact]
	public void TrackCollection_ShouldReturnBuilderForFluentAPI()
	{
		// Arrange
		var builder = new TrackerBuilder<ShoppingCart, Difference>();

		// Act
		var result = builder.TrackCollection(
			itemsSelector: c => c.Products,
			matchingPredicate: (p1, p2) => p1.Id == p2.Id,
			addedFactory: (src, tgt, item) => new Difference("Added", null, item),
			removedFactory: (src, tgt, item) => new Difference("Removed", item, null),
			configureItemTracker: itemBuilder => itemBuilder);

		// Assert
		Assert.Same(builder, result);
	}

	[Fact]
	public void Build_CanBeCalledMultipleTimes()
	{
		// Arrange
		var builder = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Name, (p, old, newVal) => new Difference("Name", old, newVal));

		// Act
		var tracker1 = builder.Build();
		var tracker2 = builder.Build();

		// Assert
		Assert.NotNull(tracker1);
		Assert.NotNull(tracker2);
		Assert.NotSame(tracker1, tracker2);
	}

	private record Product(int Id, string Name, decimal Price);
	private record ShoppingCart(List<Product> Products);
}
