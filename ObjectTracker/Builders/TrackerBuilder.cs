using ObjectTracker.Trackers;
using ObjectTracker.Trackers.ItemTrackers;

namespace ObjectTracker.Builders
{
	/// <summary>
	/// Fluent API for configuring what properties and collections to track.
	/// </summary>
	/// <typeparam name="TType">Type of object to track.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	public interface ITrackerBuilder<TType, TDiff>
	{
		/// <summary>
		/// Creates a reusable tracker from the configured tracking rules.
		/// The tracker can be used to snapshot multiple objects efficiently without rebuilding configuration.
		/// </summary>
		/// <returns>Reusable tracker instance.</returns>
		ITracker<TType, TDiff> Build();

		/// <summary>
		/// Configures tracking for a property. Differences are detected using value equality (.Equals).
		/// </summary>
		/// <typeparam name="TValue">Type of the property value.</typeparam>
		/// <param name="selector">Function to extract the property value from an object.</param>
		/// <param name="differenceFactory">Function to create a difference object. Receives the target object and both values for context.</param>
		/// <returns>Builder instance for fluent chaining.</returns>
		ITrackerBuilder<TType, TDiff> TrackProperty<TValue>(Func<TType, TValue?> selector, Func<TType, TValue?, TValue?, TDiff> differenceFactory);

		/// <summary>
		/// Configures tracking for a collection with nested item tracking.
		/// Items are matched using a predicate, enabling detection of additions, removals, and nested changes.
		/// </summary>
		/// <typeparam name="TItem">Type of items in the collection.</typeparam>
		/// <param name="itemsSelector">Function to extract the collection from an object.</param>
		/// <param name="matchingPredicate">Function to determine if two items represent the same entity (typically by ID).</param>
		/// <param name="addedFactory">Optional factory to create difference objects for added items. Receives source object, target object, and added item.</param>
		/// <param name="removedFactory">Optional factory to create difference objects for removed items. Receives source object, target object, and removed item.</param>
		/// <param name="configureItemTracker">Function to configure tracking for individual collection items (recursive tracking).</param>
		/// <returns>Builder instance for fluent chaining.</returns>
		ITrackerBuilder<TType, TDiff> TrackCollection<TItem>(Func<TType, IEnumerable<TItem>> itemsSelector, Func<TItem, TItem, bool> matchingPredicate, Func<TType, TType, TItem, TDiff>? addedFactory, Func<TType, TType, TItem, TDiff>? removedFactory, Func<ITrackerBuilder<TItem, TDiff>, ITrackerBuilder<TItem, TDiff>> configureItemTracker);
	}

	/// <summary>
	/// Fluent builder for configuring reusable object trackers.
	/// Accumulates tracking rules and builds a <see cref="Tracker{TType, TDiff}"/> that can snapshot multiple objects.
	/// </summary>
	/// <typeparam name="TType">Type of object to track.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	public class TrackerBuilder<TType, TDiff> : ITrackerBuilder<TType, TDiff>
	{
		private readonly List<IItemTracker<TType, TDiff>> _itemTrackers;

		/// <summary>
		/// Initializes a new tracker builder with no tracking rules configured.
		/// </summary>
		public TrackerBuilder()
		{
			_itemTrackers = [];
		}

		/// <inheritdoc/>
		public ITrackerBuilder<TType, TDiff> TrackProperty<TValue>(Func<TType, TValue?> selector, Func<TType, TValue?, TValue?, TDiff> differenceFactory)
		{
			var tracker = new PropertyTracker<TType, TDiff, TValue>(
				selector: selector,
				differenceFactory: differenceFactory);

			_itemTrackers.Add(tracker);

			return this;
		}

		/// <inheritdoc/>
		public ITrackerBuilder<TType, TDiff> TrackCollection<TItem>(Func<TType, IEnumerable<TItem>> itemsSelector, Func<TItem, TItem, bool> matchingPredicate, Func<TType, TType, TItem, TDiff>? addedFactory, Func<TType, TType, TItem, TDiff>? removedFactory, Func<ITrackerBuilder<TItem, TDiff>, ITrackerBuilder<TItem, TDiff>> configureItemTracker)
		{
			var initialItemTrackerBuilder = new TrackerBuilder<TItem, TDiff>();

			var configuredItemTrackerBuilder = configureItemTracker(initialItemTrackerBuilder);

			var tracker = new CollectionTracker<TType, TDiff, TItem>(
				itemsSelector: itemsSelector,
				matchingPredicate: matchingPredicate,
				addedFactory: addedFactory,
				removedFactory: removedFactory,
				itemTracker: configuredItemTrackerBuilder.Build());

			_itemTrackers.Add(tracker);

			return this;

		}

		/// <inheritdoc/>
		public ITracker<TType, TDiff> Build()
		{
			return new Tracker<TType, TDiff>(_itemTrackers);
		}
	}
}
