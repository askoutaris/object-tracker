using ObjectTracker.Builders;

namespace ObjectTracker.Tests;

public class TrackerTests
{
	private record Person(string Name, int Age, string? Email);
	private record Difference(string PropertyName, object? OldValue, object? NewValue);

	[Fact]
	public void Track_ShouldReturnTrackedObject()
	{
		// Arrange
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Name, (p, old, newVal) => new Difference("Name", old, newVal))
			.Build();
		var person = new Person("John", 30, "john@example.com");

		// Act
		var trackedObject = tracker.Track(person);

		// Assert
		Assert.NotNull(trackedObject);
		Assert.Equal(person, trackedObject.Source);
	}

	[Fact]
	public void Track_CanTrackMultipleObjectsWithSameTracker()
	{
		// Arrange
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Name, (p, old, newVal) => new Difference("Name", old, newVal))
			.Build();
		var person1 = new Person("John", 30, "john@example.com");
		var person2 = new Person("Jane", 25, "jane@example.com");

		// Act
		var tracked1 = tracker.Track(person1);
		var tracked2 = tracker.Track(person2);

		// Assert
		Assert.NotSame(tracked1, tracked2);
		Assert.Equal(person1, tracked1.Source);
		Assert.Equal(person2, tracked2.Source);
	}

	[Fact]
	public void GetDifferences_WhenNoChanges_ShouldReturnEmpty()
	{
		// Arrange
		var person = new Person("John", 30, "john@example.com");
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Name, (p, old, newVal) => new Difference("Name", old, newVal))
			.TrackProperty(p => p.Age, (p, old, newVal) => new Difference("Age", old, newVal))
			.Build();
		var trackedPerson = tracker.Track(person);

		// Act
		var differences = trackedPerson.GetDifferences();

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void GetDifferences_WhenPropertyChanged_ShouldReturnDifference()
	{
		// Arrange
		var person = new Person("John", 30, "john@example.com");
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Name, (p, old, newVal) => new Difference("Name", old, newVal))
			.TrackProperty(p => p.Age, (p, old, newVal) => new Difference("Age", old, newVal))
			.Build();
		var trackedPerson = tracker.Track(person);

		// Act - modify the person (in real scenarios, person would be mutable)
		var modifiedPerson = person with { Age = 31 };
		var differences = trackedPerson.Compare(modifiedPerson);

		// Assert
		Assert.Single(differences);
		var diff = differences.First();
		Assert.Equal("Age", diff.PropertyName);
		Assert.Equal(30, diff.OldValue);
		Assert.Equal(31, diff.NewValue);
	}

	[Fact]
	public void Compare_WhenMultiplePropertiesChanged_ShouldReturnMultipleDifferences()
	{
		// Arrange
		var person = new Person("John", 30, "john@example.com");
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Name, (p, old, newVal) => new Difference("Name", old, newVal))
			.TrackProperty(p => p.Age, (p, old, newVal) => new Difference("Age", old, newVal))
			.TrackProperty(p => p.Email, (p, old, newVal) => new Difference("Email", old, newVal))
			.Build();
		var trackedPerson = tracker.Track(person);

		// Act
		var modifiedPerson = new Person("Jane", 31, "jane@example.com");
		var differences = trackedPerson.Compare(modifiedPerson);

		// Assert
		Assert.Equal(3, differences.Count);
		Assert.Contains(differences, d => d.PropertyName == "Name" && (string)d.OldValue! == "John" && (string)d.NewValue! == "Jane");
		Assert.Contains(differences, d => d.PropertyName == "Age" && (int)d.OldValue! == 30 && (int)d.NewValue! == 31);
		Assert.Contains(differences, d => d.PropertyName == "Email");
	}

	[Fact]
	public void Compare_WhenValueChangedFromNullToValue_ShouldReturnDifference()
	{
		// Arrange
		var person = new Person("John", 30, null);
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Email, (p, old, newVal) => new Difference("Email", old, newVal))
			.Build();
		var trackedPerson = tracker.Track(person);

		// Act
		var modifiedPerson = person with { Email = "john@example.com" };
		var differences = trackedPerson.Compare(modifiedPerson);

		// Assert
		Assert.Single(differences);
		Assert.Equal("Email", differences.First().PropertyName);
		Assert.Null(differences.First().OldValue);
		Assert.Equal("john@example.com", differences.First().NewValue);
	}

	[Fact]
	public void Compare_WhenValueChangedFromValueToNull_ShouldReturnDifference()
	{
		// Arrange
		var person = new Person("John", 30, "john@example.com");
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Email, (p, old, newVal) => new Difference("Email", old, newVal))
			.Build();
		var trackedPerson = tracker.Track(person);

		// Act
		var modifiedPerson = person with { Email = null };
		var differences = trackedPerson.Compare(modifiedPerson);

		// Assert
		Assert.Single(differences);
		Assert.Equal("Email", differences.First().PropertyName);
		Assert.Equal("john@example.com", differences.First().OldValue);
		Assert.Null(differences.First().NewValue);
	}

	[Fact]
	public void Compare_WhenBothValuesAreNull_ShouldReturnNoDifferences()
	{
		// Arrange
		var person = new Person("John", 30, null);
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Email, (p, old, newVal) => new Difference("Email", old, newVal))
			.Build();
		var trackedPerson = tracker.Track(person);

		// Act
		var modifiedPerson = person with { Email = null };
		var differences = trackedPerson.Compare(modifiedPerson);

		// Assert
		Assert.Empty(differences);
	}

	[Fact]
	public void Compare_WithComplexSelector_ShouldTrackCorrectly()
	{
		// Arrange
		var person = new Person("John", 30, "john@example.com");
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Name.Length, (p, old, newVal) => new Difference("NameLength", old, newVal))
			.Build();
		var trackedPerson = tracker.Track(person);

		// Act
		var modifiedPerson = person with { Name = "Alexander" };
		var differences = trackedPerson.Compare(modifiedPerson);

		// Assert
		Assert.Single(differences);
		Assert.Equal("NameLength", differences.First().PropertyName);
		Assert.Equal(4, differences.First().OldValue);
		Assert.Equal(9, differences.First().NewValue);
	}

	[Fact]
	public void Compare_CalledMultipleTimes_ShouldAlwaysCompareAgainstOriginalSource()
	{
		// Arrange
		var person = new Person("John", 30, "john@example.com");
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Age, (p, old, newVal) => new Difference("Age", old, newVal))
			.Build();
		var trackedPerson = tracker.Track(person);

		// Act
		var modified1 = person with { Age = 31 };
		var modified2 = person with { Age = 32 };

		var differences1 = trackedPerson.Compare(modified1);
		var differences2 = trackedPerson.Compare(modified2);

		// Assert
		Assert.Single(differences1);
		Assert.Equal(30, differences1.First().OldValue);
		Assert.Equal(31, differences1.First().NewValue);

		Assert.Single(differences2);
		Assert.Equal(30, differences2.First().OldValue); // Still comparing against original
		Assert.Equal(32, differences2.First().NewValue);
	}

	[Fact]
	public void DifferenceFactory_ReceivesTargetObjectForContext()
	{
		// Arrange
		Person? capturedPerson = null;
		var person = new Person("John", 30, "john@example.com");
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(
				p => p.Age,
				(p, old, newVal) =>
				{
					capturedPerson = p;
					return new Difference("Age", old, newVal);
				})
			.Build();
		var trackedPerson = tracker.Track(person);

		// Act
		var modified = person with { Age = 31 };
		trackedPerson.Compare(modified);

		// Assert
		Assert.NotNull(capturedPerson);
		Assert.Equal(modified, capturedPerson);
	}

	[Fact]
	public void TrackerReuse_ShouldProduceIndependentTrackedObjects()
	{
		// Arrange
		var tracker = new TrackerBuilder<Person, Difference>()
			.TrackProperty(p => p.Age, (p, old, newVal) => new Difference("Age", old, newVal))
			.Build();

		var person1 = new Person("John", 30, "john@example.com");
		var person2 = new Person("Jane", 25, "jane@example.com");

		// Act
		var tracked1 = tracker.Track(person1);
		var tracked2 = tracker.Track(person2);

		var modified1 = person1 with { Age = 31 };
		var modified2 = person2 with { Age = 26 };

		var diff1 = tracked1.Compare(modified1);
		var diff2 = tracked2.Compare(modified2);

		// Assert
		Assert.Single(diff1);
		Assert.Equal(30, diff1.First().OldValue);
		Assert.Equal(31, diff1.First().NewValue);

		Assert.Single(diff2);
		Assert.Equal(25, diff2.First().OldValue);
		Assert.Equal(26, diff2.First().NewValue);
	}
}
