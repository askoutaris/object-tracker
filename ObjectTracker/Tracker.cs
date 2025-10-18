namespace ObjectTracker
{
	/// <summary>
	/// Provides methods to configure tracking and compare objects against a captured snapshot.
	/// </summary>
	/// <typeparam name="TType">The type of object being tracked.</typeparam>
	/// <typeparam name="TDiff">The type used to represent differences.</typeparam>
	public interface ITracker<TType, TDiff>
	{
		/// <summary>
		/// Configures tracking for a property or computed value. When the selected value differs between source and target, the factory creates a difference.
		/// </summary>
		/// <typeparam name="TValue">The type of value being tracked.</typeparam>
		/// <param name="selector">Function to extract the value to track from the object.</param>
		/// <param name="differenceFactory">Function to create a difference when old and new values don't match. Receives (oldValue, newValue).</param>
		/// <returns>The tracker instance for method chaining.</returns>
		Tracker<TType, TDiff> TrackProperty<TValue>(Func<TType, TValue> selector, Func<TValue?, TValue?, TDiff> differenceFactory);

		/// <summary>
		/// Configures tracking for a collection of items. Detects additions, removals, and changes to matched items.
		/// </summary>
		/// <typeparam name="TItem">The type of items in the collection.</typeparam>
		/// <param name="itemsSelector">Function to extract the collection from the object.</param>
		/// <param name="matchingPredicate">Function to determine if two items are the same. Receives (sourceItem, targetItem).</param>
		/// <param name="addedFactory">Optional. Function to create a difference for items in target but not in source. Receives (source, target, addedItem).</param>
		/// <param name="removedFactory">Optional. Function to create a difference for items in source but not in target. Receives (source, target, removedItem).</param>
		/// <param name="configureTracker">Optional. Action to configure property tracking for matched items within the collection.</param>
		/// <returns>The tracker instance for method chaining.</returns>
		Tracker<TType, TDiff> TrackCollection<TItem>(Func<TType, IEnumerable<TItem>> itemsSelector, Func<TItem, TItem, bool> matchingPredicate, Func<TType, TType, TItem, TDiff>? addedFactory = null, Func<TType, TType, TItem, TDiff>? removedFactory = null, Action<Tracker<TItem, TDiff>>? configureTracker = null);

		/// <summary>
		/// Compares the target object against the original snapshot and returns all detected differences.
		/// </summary>
		/// <param name="target">The object to compare against the snapshot.</param>
		/// <returns>Array of differences detected across all tracked properties and collections.</returns>
		TDiff[] Compare(TType target);
	}

	/// <summary>
	/// Tracks an object's state and detects changes by comparing against a captured snapshot.
	/// Supports property tracking, collection tracking with add/remove/modify detection, and nested tracking.
	/// </summary>
	/// <typeparam name="TType">The type of object being tracked.</typeparam>
	/// <typeparam name="TDiff">The type used to represent differences.</typeparam>
	public partial class Tracker<TType, TDiff> : ITracker<TType, TDiff>
	{
		interface ITrackedItem
		{
			TDiff[] Compare(TType target);
		}

		private readonly List<ITrackedItem> _trackedItems;

		/// <summary>
		/// Gets the original source object that was captured when the tracker was created.
		/// </summary>
		public TType Source { get; }

		/// <summary>
		/// Initializes a new tracker with the source object. Use <see cref="CreateNew"/> to create instances.
		/// </summary>
		/// <param name="source">The object to track.</param>
		protected Tracker(TType source)
		{
			_trackedItems = [];
			Source = source;
		}

		/// <summary>
		/// Creates a new tracker and captures a snapshot of the source object's current state.
		/// </summary>
		/// <param name="source">The object to track.</param>
		/// <returns>A configured tracker instance ready for property and collection tracking setup.</returns>
		public static ITracker<TType, TDiff> CreateNew(TType source)
			=> new Tracker<TType, TDiff>(source);

		/// <summary>
		/// Configures tracking for a property or computed value. When the selected value differs between source and target, the factory creates a difference.
		/// Values are compared using <see cref="object.Equals(object?)"/>. Null transitions (null to value, value to null) are always detected.
		/// </summary>
		/// <typeparam name="TValue">The type of value being tracked.</typeparam>
		/// <param name="selector">Function to extract the value to track from the object.</param>
		/// <param name="differenceFactory">Function to create a difference when old and new values don't match. Receives (oldValue, newValue).</param>
		/// <returns>The tracker instance for method chaining.</returns>
		public Tracker<TType, TDiff> TrackProperty<TValue>(
			Func<TType, TValue> selector,
			Func<TValue?, TValue?, TDiff> differenceFactory)
		{
			_trackedItems.Add(new TrackedProperty<TValue>(Source, selector, differenceFactory));
			return this;
		}

		/// <summary>
		/// Configures tracking for a collection of items. Detects additions, removals, and changes to matched items.
		/// Items are matched using the predicate. Unmatched items in target are additions; unmatched in source are removals.
		/// Use <paramref name="configureTracker"/> to recursively track properties within matched items.
		/// </summary>
		/// <typeparam name="TItem">The type of items in the collection.</typeparam>
		/// <param name="itemsSelector">Function to extract the collection from the object.</param>
		/// <param name="matchingPredicate">Function to determine if two items are the same. Receives (sourceItem, targetItem). Typically compares by ID or key.</param>
		/// <param name="addedFactory">Optional. Function to create a difference for items in target but not in source. Receives (source, target, addedItem). If null, additions are not reported.</param>
		/// <param name="removedFactory">Optional. Function to create a difference for items in source but not in target. Receives (source, target, removedItem). If null, removals are not reported.</param>
		/// <param name="configureTracker">Optional. Action to configure property tracking for matched items within the collection. Receives a tracker for each source item.</param>
		/// <returns>The tracker instance for method chaining.</returns>
		public Tracker<TType, TDiff> TrackCollection<TItem>(
			Func<TType, IEnumerable<TItem>> itemsSelector,
			Func<TItem, TItem, bool> matchingPredicate,
			Func<TType, TType, TItem, TDiff>? addedFactory = null,
			Func<TType, TType, TItem, TDiff>? removedFactory = null,
			Action<Tracker<TItem, TDiff>>? configureTracker = null)
		{
			_trackedItems.Add(new TrackedCollection<TItem>(Source, itemsSelector, matchingPredicate, addedFactory, removedFactory, configureTracker));

			return this;
		}

		/// <summary>
		/// Compares the target object against the original snapshot and returns all detected differences.
		/// The snapshot is immutable - multiple calls always compare against the same original state.
		/// </summary>
		/// <param name="target">The object to compare against the snapshot. Can be the same instance if it was mutated after tracking began.</param>
		/// <returns>Array of differences detected across all tracked properties and collections. Empty if no changes detected.</returns>
		public TDiff[] Compare(TType target)
		{
			return [.. _trackedItems.SelectMany(value => value.Compare(target))];
		}
	}
}
