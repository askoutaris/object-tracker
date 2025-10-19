using ObjectTracker.TrackedObjects.TrackedItems;

namespace ObjectTracker.Trackers.ItemTrackers
{
	/// <summary>
	/// Reusable configuration for tracking a collection with nested item tracking.
	/// Holds collection selector, matching predicate, factories, and a nested item tracker for recursive tracking.
	/// Creates <see cref="TrackedCollection{TType, TDiff, TItem}"/> instances to capture collection snapshots.
	/// Stateless and reusable across multiple objects.
	/// </summary>
	/// <typeparam name="TType">Type of object containing the collection.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	/// <typeparam name="TItem">Type of items in the collection.</typeparam>
	class CollectionTracker<TType, TDiff, TItem> : IItemTracker<TType, TDiff>
	{
		private readonly Func<TType, IEnumerable<TItem>> _itemsSelector;
		private readonly Func<TItem, TItem, bool> _matchingPredicate;
		private readonly Func<TType, TType, TItem, TDiff>? _addedFactory;
		private readonly Func<TType, TType, TItem, TDiff>? _removedFactory;
		private readonly ITracker<TItem, TDiff> _itemTracker;

		/// <summary>
		/// Initializes a collection tracker with selectors, predicates, factories, and nested item tracker.
		/// </summary>
		/// <param name="itemsSelector">Function to extract the collection from an object.</param>
		/// <param name="matchingPredicate">Function to determine if two items represent the same entity.</param>
		/// <param name="addedFactory">Optional factory to create difference objects for added items.</param>
		/// <param name="removedFactory">Optional factory to create difference objects for removed items.</param>
		/// <param name="itemTracker">Nested tracker to capture snapshots of individual collection items (enables recursive tracking).</param>
		public CollectionTracker(
			Func<TType, IEnumerable<TItem>> itemsSelector,
			Func<TItem, TItem, bool> matchingPredicate,
			Func<TType, TType, TItem, TDiff>? addedFactory,
			Func<TType, TType, TItem, TDiff>? removedFactory,
			ITracker<TItem, TDiff> itemTracker)
		{
			_itemsSelector = itemsSelector;
			_matchingPredicate = matchingPredicate;
			_addedFactory = addedFactory;
			_removedFactory = removedFactory;
			_itemTracker = itemTracker;
		}

		/// <inheritdoc/>
		public ITrackedItem<TType, TDiff> GetTrackedItem(TType source)
		{
			var trackedItems = _itemsSelector(source)
				.Select(_itemTracker.Track)
				.ToArray();

			return new TrackedCollection<TType, TDiff, TItem>(
				source: source,
				itemsSelector: _itemsSelector,
				matchingPredicate: _matchingPredicate,
				addedFactory: _addedFactory,
				removedFactory: _removedFactory,
				trackedItems: trackedItems);
		}
	}
}
