using ObjectTracker.Builders;

namespace ObjectTracker.Tests;

public class CollectionTrackerTests
{
	private record Product(int Id, string Name, decimal Price);
	private record ShoppingCart(List<Product> Products);
	private record Difference(string Type, object? Data);

	[Fact]
	public void TrackCollection_WhenNoItemsAddedOrRemoved_ShouldReturnEmpty()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureItemTracker: builder => builder)
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var differences = trackedCart.GetDifferences();

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void Compare_WhenItemAdded_ShouldReturnAddedDifference()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureItemTracker: builder => builder)
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Single(differences);
		Assert.Equal("Added", differences.First().Type);
		var addedProduct = (Product)differences.First().Data!;
		Assert.Equal(2, addedProduct.Id);
		Assert.Equal("Banana", addedProduct.Name);
	}

	[Fact]
	public void Compare_WhenItemRemoved_ShouldReturnRemovedDifference()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureItemTracker: builder => builder)
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Single(differences);
		Assert.Equal("Removed", differences.First().Type);
		var removedProduct = (Product)differences.First().Data!;
		Assert.Equal(2, removedProduct.Id);
		Assert.Equal("Banana", removedProduct.Name);
	}

	[Fact]
	public void Compare_WhenMultipleItemsAdded_ShouldReturnMultipleDifferences()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureItemTracker: builder => builder)
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m),
			new Product(3, "Orange", 1.20m)
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Equal(2, differences.Count);
		Assert.All(differences, d => Assert.Equal("Added", d.Type));
	}

	[Fact]
	public void Compare_WhenItemsAddedAndRemoved_ShouldReturnBothDifferences()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureItemTracker: builder => builder)
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(3, "Orange", 1.20m)
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Equal(2, differences.Count);
		Assert.Contains(differences, d => d.Type == "Removed" && ((Product)d.Data!).Id == 2);
		Assert.Contains(differences, d => d.Type == "Added" && ((Product)d.Data!).Id == 3);
	}

	[Fact]
	public void Compare_WithItemPropertyChanges_ShouldDetectNestedChanges()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureItemTracker: builder => builder
					.TrackProperty(
						p => p.Price,
						(p, old, newVal) => new Difference($"PriceChanged", new { OldPrice = old, NewPrice = newVal })))
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.75m), // Price changed
			new Product(2, "Banana", 0.80m)
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Single(differences);
		Assert.Equal("PriceChanged", differences.First().Type);
	}

	[Fact]
	public void Compare_WithItemPropertyChangesOnMultipleItems_ShouldDetectAll()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: null,
				removedFactory: null,
				configureItemTracker: builder => builder
					.TrackProperty(p => p.Price, (p, old, newVal) => new Difference($"PriceChanged-{p.Id}", new { OldPrice = old, NewPrice = newVal }))
					.TrackProperty(p => p.Name, (p, old, newVal) => new Difference($"NameChanged-{p.Id}", new { OldName = old, NewName = newVal })))
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Granny Smith Apple", 1.75m), // Name and price changed
			new Product(2, "Banana", 0.90m) // Price changed
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Equal(3, differences.Count);
		Assert.Contains(differences, d => d.Type == "PriceChanged-1");
		Assert.Contains(differences, d => d.Type == "NameChanged-1");
		Assert.Contains(differences, d => d.Type == "PriceChanged-2");
	}

	[Fact]
	public void Compare_WhenNoAddedFactoryProvided_ShouldNotReportAddedItems()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: null,
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureItemTracker: builder => builder)
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void Compare_WhenNoRemovedFactoryProvided_ShouldNotReportRemovedItems()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: null,
				configureItemTracker: builder => builder)
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void Compare_WithEmptySourceCollection_ShouldReportAllAsAdded()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>());

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureItemTracker: builder => builder)
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Equal(2, differences.Count);
		Assert.All(differences, d => Assert.Equal("Added", d.Type));
	}

	[Fact]
	public void Compare_WithEmptyTargetCollection_ShouldReportAllAsRemoved()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureItemTracker: builder => builder)
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>());
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Equal(2, differences.Count);
		Assert.All(differences, d => Assert.Equal("Removed", d.Type));
	}

	[Fact]
	public void Compare_WithComplexAddRemoveAndModify_ShouldDetectAllChanges()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m),
			new Product(3, "Orange", 1.20m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureItemTracker: builder => builder
					.TrackProperty(p => p.Price, (p, old, newVal) => new Difference("PriceChanged", new { Id = p.Id, OldPrice = old, NewPrice = newVal })))
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.75m), // Modified
			new Product(3, "Orange", 1.20m), // Unchanged
			new Product(4, "Grape", 2.00m)   // Added
			// Product 2 (Banana) removed
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Equal(3, differences.Count);
		Assert.Contains(differences, d => d.Type == "PriceChanged");
		Assert.Contains(differences, d => d.Type == "Removed" && ((Product)d.Data!).Id == 2);
		Assert.Contains(differences, d => d.Type == "Added" && ((Product)d.Data!).Id == 4);
	}

	[Fact]
	public void TrackCollection_CanBeCombinedWithTrackProperty_ShouldTrackBoth()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		});

		var tracker = new TrackerBuilder<ShoppingCart, Difference>()
			.TrackProperty(c => c.Products.Count, (c, old, newVal) => new Difference("CountChanged", new { OldCount = old, NewCount = newVal }))
			.TrackCollection(
				itemsSelector: c => c.Products,
				matchingPredicate: (p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: null,
				configureItemTracker: builder => builder)
			.Build();

		var trackedCart = tracker.Track(cart);

		// Act
		var modifiedCart = new ShoppingCart(new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		});
		var differences = trackedCart.Compare(modifiedCart);

		// Assert
		Assert.Equal(2, differences.Count);
		Assert.Contains(differences, d => d.Type == "CountChanged");
		Assert.Contains(differences, d => d.Type == "Added");
	}
}
