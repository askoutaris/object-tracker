namespace ObjectTracker.Tests;

public class TrackerTests
{
	private record Person(string Name, int Age, string? Email);
	private record Difference(string PropertyName, object? OldValue, object? NewValue);

	[Fact]
	public void CreateNew_ShouldCreateTracker()
	{
		// Arrange
		var person = new Person("John", 30, "john@example.com");

		// Act
		var tracker = Tracker<Person, Difference>.CreateNew(person);

		// Assert
		Assert.NotNull(tracker);
	}

	[Fact]
	public void Track_ShouldReturnTrackerForFluentAPI()
	{
		// Arrange
		var person = new Person("John", 30, null);
		var tracker = Tracker<Person, Difference>.CreateNew(person);

		// Act
		var result = tracker.TrackProperty(p => p.Name, (old, newVal) => new Difference("Name", old, newVal));

		// Assert
		Assert.Same(tracker, result);
	}

	[Fact]
	public void Compare_WhenNoChanges_ShouldReturnEmptyArray()
	{
		// Arrange
		var person = new Person("John", 30, "john@example.com");
		var tracker = Tracker<Person, Difference>.CreateNew(person)
			.TrackProperty(p => p.Name, (old, newVal) => new Difference("Name", old, newVal))
			.TrackProperty(p => p.Age, (old, newVal) => new Difference("Age", old, newVal))
			.TrackProperty(p => p.Email, (old, newVal) => new Difference("Email", old, newVal));

		// Act
		var differences = tracker.Compare(person);

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void Compare_WhenSinglePropertyChanged_ShouldReturnOneDifference()
	{
		// Arrange
		var original = new Person("John", 30, "john@example.com");
		var tracker = Tracker<Person, Difference>.CreateNew(original)
			.TrackProperty(p => p.Name, (old, newVal) => new Difference("Name", old, newVal))
			.TrackProperty(p => p.Age, (old, newVal) => new Difference("Age", old, newVal));

		var modified = new Person("John", 31, "john@example.com");

		// Act
		var differences = tracker.Compare(modified);

		// Assert
		Assert.Single(differences);
		Assert.Equal("Age", differences[0].PropertyName);
		Assert.Equal(30, differences[0].OldValue);
		Assert.Equal(31, differences[0].NewValue);
	}

	[Fact]
	public void Compare_WhenMultiplePropertiesChanged_ShouldReturnMultipleDifferences()
	{
		// Arrange
		var original = new Person("John", 30, "john@example.com");
		var tracker = Tracker<Person, Difference>.CreateNew(original)
			.TrackProperty(p => p.Name, (old, newVal) => new Difference("Name", old, newVal))
			.TrackProperty(p => p.Age, (old, newVal) => new Difference("Age", old, newVal))
			.TrackProperty(p => p.Email, (old, newVal) => new Difference("Email", old, newVal));

		var modified = new Person("Jane", 31, "john@example.com");

		// Act
		var differences = tracker.Compare(modified);

		// Assert
		Assert.Equal(2, differences.Length);
		Assert.Contains(differences, d => d.PropertyName == "Name" && (string)d.OldValue! == "John" && (string)d.NewValue! == "Jane");
		Assert.Contains(differences, d => d.PropertyName == "Age" && (int)d.OldValue! == 30 && (int)d.NewValue! == 31);
	}

	[Fact]
	public void Compare_WhenValueChangedFromNullToValue_ShouldReturnDifference()
	{
		// Arrange
		var original = new Person("John", 30, null);
		var tracker = Tracker<Person, Difference>.CreateNew(original)
			.TrackProperty(p => p.Email, (old, newVal) => new Difference("Email", old, newVal));

		var modified = new Person("John", 30, "john@example.com");

		// Act
		var differences = tracker.Compare(modified);

		// Assert
		Assert.Single(differences);
		Assert.Equal("Email", differences[0].PropertyName);
		Assert.Null(differences[0].OldValue);
		Assert.Equal("john@example.com", differences[0].NewValue);
	}

	[Fact]
	public void Compare_WhenValueChangedFromValueToNull_ShouldReturnDifference()
	{
		// Arrange
		var original = new Person("John", 30, "john@example.com");
		var tracker = Tracker<Person, Difference>.CreateNew(original)
			.TrackProperty(p => p.Email, (old, newVal) => new Difference("Email", old, newVal));

		var modified = new Person("John", 30, null);

		// Act
		var differences = tracker.Compare(modified);

		// Assert
		Assert.Single(differences);
		Assert.Equal("Email", differences[0].PropertyName);
		Assert.Equal("john@example.com", differences[0].OldValue);
		Assert.Null(differences[0].NewValue);
	}

	[Fact]
	public void Compare_WhenBothValuesAreNull_ShouldReturnNoDifferences()
	{
		// Arrange
		var original = new Person("John", 30, null);
		var tracker = Tracker<Person, Difference>.CreateNew(original)
			.TrackProperty(p => p.Email, (old, newVal) => new Difference("Email", old, newVal));

		var modified = new Person("John", 30, null);

		// Act
		var differences = tracker.Compare(modified);

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void Compare_WhenNoPropertiesTracked_ShouldReturnEmptyArray()
	{
		// Arrange
		var original = new Person("John", 30, "john@example.com");
		var tracker = Tracker<Person, Difference>.CreateNew(original);

		var modified = new Person("Jane", 31, "jane@example.com");

		// Act
		var differences = tracker.Compare(modified);

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void Compare_WithComplexSelector_ShouldTrackCorrectly()
	{
		// Arrange
		var original = new Person("John", 30, "john@example.com");
		var tracker = Tracker<Person, Difference>.CreateNew(original)
			.TrackProperty(p => p.Name.Length, (old, newVal) => new Difference("NameLength", old, newVal));

		var modified = new Person("Alexander", 30, "john@example.com");

		// Act
		var differences = tracker.Compare(modified);

		// Assert
		Assert.Single(differences);
		Assert.Equal("NameLength", differences[0].PropertyName);
		Assert.Equal(4, differences[0].OldValue);
		Assert.Equal(9, differences[0].NewValue);
	}

	[Fact]
	public void Compare_CalledMultipleTimes_ShouldAlwaysCompareAgainstOriginalSource()
	{
		// Arrange
		var original = new Person("John", 30, "john@example.com");
		var tracker = Tracker<Person, Difference>.CreateNew(original)
			.TrackProperty(p => p.Age, (old, newVal) => new Difference("Age", old, newVal));

		var modified1 = new Person("John", 31, "john@example.com");
		var modified2 = new Person("John", 32, "john@example.com");

		// Act
		var differences1 = tracker.Compare(modified1);
		var differences2 = tracker.Compare(modified2);

		// Assert
		Assert.Single(differences1);
		Assert.Equal(30, differences1[0].OldValue);
		Assert.Equal(31, differences1[0].NewValue);

		Assert.Single(differences2);
		Assert.Equal(30, differences2[0].OldValue); // Still comparing against original
		Assert.Equal(32, differences2[0].NewValue);
	}
}
