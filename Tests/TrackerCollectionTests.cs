namespace ObjectTracker.Tests;

public class TrackerCollectionTests
{
	private record Product(int Id, string Name, decimal Price);
	private record ShoppingCart(List<Product> Products);
	private record Difference(string Type, object? Data);

	[Fact]
	public void TrackItems_ShouldReturnTrackerForFluentAPI()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>());
		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart);

		// Act
		var result = tracker.TrackItems(
			c => c.Products,
			(p1, p2) => p1.Id == p2.Id,
			addedFactory: (src, tgt, item) => new Difference("Added", item),
			removedFactory: (src, tgt, item) => new Difference("Removed", item));

		// Assert
		Assert.Same(tracker, result);
	}

	[Fact]
	public void Compare_WhenNoItemsAddedOrRemoved_ShouldReturnEmptyArray()
	{
		// Arrange
		var products = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var cart = new ShoppingCart(products);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item));

		// Act
		var differences = tracker.Compare(cart);

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void Compare_WhenItemAdded_ShouldReturnAddedDifference()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item));

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Single(differences);
		Assert.Equal("Added", differences[0].Type);
		var addedProduct = (Product)differences[0].Data!;
		Assert.Equal(2, addedProduct.Id);
		Assert.Equal("Banana", addedProduct.Name);
	}

	[Fact]
	public void Compare_WhenItemRemoved_ShouldReturnRemovedDifference()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item));

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Single(differences);
		Assert.Equal("Removed", differences[0].Type);
		var removedProduct = (Product)differences[0].Data!;
		Assert.Equal(2, removedProduct.Id);
		Assert.Equal("Banana", removedProduct.Name);
	}

	[Fact]
	public void Compare_WhenMultipleItemsAdded_ShouldReturnMultipleDifferences()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item));

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m),
			new Product(3, "Orange", 1.20m)
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Equal(2, differences.Length);
		Assert.All(differences, d => Assert.Equal("Added", d.Type));
	}

	[Fact]
	public void Compare_WhenItemsAddedAndRemoved_ShouldReturnBothDifferences()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item));

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(3, "Orange", 1.20m)
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Equal(2, differences.Length);
		Assert.Contains(differences, d => d.Type == "Removed" && ((Product)d.Data!).Id == 2);
		Assert.Contains(differences, d => d.Type == "Added" && ((Product)d.Data!).Id == 3);
	}

	[Fact]
	public void Compare_WithItemPropertyChanges_ShouldDetectNestedChanges()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureTracker: itemTracker =>
				{
					itemTracker.Track(p => p.Price, (old, newVal) => new Difference($"PriceChanged", new { OldPrice = old, NewPrice = newVal }));
				});

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Apple", 1.75m), // Price changed
			new Product(2, "Banana", 0.80m)
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Single(differences);
		Assert.Equal("PriceChanged", differences[0].Type);
	}

	[Fact]
	public void Compare_WithItemPropertyChangesOnMultipleItems_ShouldDetectAll()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				configureTracker: itemTracker =>
				{
					itemTracker.Track(p => p.Price, (old, newVal) => new Difference($"PriceChanged-{itemTracker.Source.Id}", new { OldPrice = old, NewPrice = newVal }));
					itemTracker.Track(p => p.Name, (old, newVal) => new Difference($"NameChanged-{itemTracker.Source.Id}", new { OldName = old, NewName = newVal }));
				});

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Granny Smith Apple", 1.75m), // Name and price changed
			new Product(2, "Banana", 0.90m) // Price changed
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Equal(3, differences.Length);
		Assert.Contains(differences, d => d.Type == "PriceChanged-1");
		Assert.Contains(differences, d => d.Type == "NameChanged-1");
		Assert.Contains(differences, d => d.Type == "PriceChanged-2");
	}

	[Fact]
	public void Compare_WhenNoAddedFactoryProvided_ShouldNotReportAddedItems()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				removedFactory: (src, tgt, item) => new Difference("Removed", item));

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void Compare_WhenNoRemovedFactoryProvided_ShouldNotReportRemovedItems()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item));

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void Compare_WithEmptySourceCollection_ShouldReportAllAsAdded()
	{
		// Arrange
		var cart = new ShoppingCart(new List<Product>());

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item));

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Equal(2, differences.Length);
		Assert.All(differences, d => Assert.Equal("Added", d.Type));
	}

	[Fact]
	public void Compare_WithEmptyTargetCollection_ShouldReportAllAsRemoved()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item));

		var modifiedCart = new ShoppingCart(new List<Product>());

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Equal(2, differences.Length);
		Assert.All(differences, d => Assert.Equal("Removed", d.Type));
	}

	[Fact]
	public void Compare_WithComplexAddRemoveAndModify_ShouldDetectAllChanges()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m),
			new Product(3, "Orange", 1.20m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item),
				removedFactory: (src, tgt, item) => new Difference("Removed", item),
				configureTracker: itemTracker =>
				{
					itemTracker.Track(p => p.Price, (old, newVal) => new Difference("PriceChanged", new { Id = itemTracker.Source.Id, OldPrice = old, NewPrice = newVal }));
				});

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Apple", 1.75m), // Modified
			new Product(3, "Orange", 1.20m), // Unchanged
			new Product(4, "Grape", 2.00m)   // Added
			// Product 2 (Banana) removed
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Equal(3, differences.Length);
		Assert.Contains(differences, d => d.Type == "PriceChanged");
		Assert.Contains(differences, d => d.Type == "Removed" && ((Product)d.Data!).Id == 2);
		Assert.Contains(differences, d => d.Type == "Added" && ((Product)d.Data!).Id == 4);
	}

	[Fact]
	public void TrackItems_CanBeCombinedWithTrack_ShouldTrackBoth()
	{
		// Arrange
		var originalProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m)
		};
		var cart = new ShoppingCart(originalProducts);

		var tracker = Tracker<ShoppingCart, Difference>.CreateNew(cart)
			.Track(c => c.Products.Count, (old, newVal) => new Difference("CountChanged", new { OldCount = old, NewCount = newVal }))
			.TrackItems(
				c => c.Products,
				(p1, p2) => p1.Id == p2.Id,
				addedFactory: (src, tgt, item) => new Difference("Added", item));

		var modifiedProducts = new List<Product>
		{
			new Product(1, "Apple", 1.50m),
			new Product(2, "Banana", 0.80m)
		};
		var modifiedCart = new ShoppingCart(modifiedProducts);

		// Act
		var differences = tracker.Compare(modifiedCart);

		// Assert
		Assert.Equal(2, differences.Length);
		Assert.Contains(differences, d => d.Type == "CountChanged");
		Assert.Contains(differences, d => d.Type == "Added");
	}
}
