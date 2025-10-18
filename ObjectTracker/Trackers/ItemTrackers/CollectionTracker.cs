using ObjectTracker.TrackedObjects.TrackedItems;

namespace ObjectTracker.Trackers.ItemTrackers
{
	class CollectionTracker<TType, TDiff, TItem> : IItemTracker<TType, TDiff>
	{
		private readonly Func<TType, IEnumerable<TItem>> _itemsSelector;
		private readonly Func<TItem, TItem, bool> _matchingPredicate;
		private readonly Func<TType, TType, TItem, TDiff>? _addedFactory;
		private readonly Func<TType, TType, TItem, TDiff>? _removedFactory;
		private readonly ITracker<TItem, TDiff> _itemTracker;

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
