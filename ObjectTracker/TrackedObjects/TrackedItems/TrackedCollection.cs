namespace ObjectTracker.TrackedObjects.TrackedItems
{
	class TrackedCollection<TType, TDiff, TItem> : ITrackedItem<TType, TDiff>
	{
		private readonly TType _source;

		private readonly Func<TType, IEnumerable<TItem>> _itemsSelector;
		private readonly Func<TItem, TItem, bool> _matchingPredicate;
		private readonly Func<TType, TType, TItem, TDiff>? _addedFactory;
		private readonly Func<TType, TType, TItem, TDiff>? _removedFactory;
		private readonly IReadOnlyCollection<ITrackedObject<TItem, TDiff>> _trackedItems;

		public TrackedCollection(
			TType source,
			Func<TType, IEnumerable<TItem>> itemsSelector,
			Func<TItem, TItem, bool> matchingPredicate,
			Func<TType, TType, TItem, TDiff>? addedFactory,
			Func<TType, TType, TItem, TDiff>? removedFactory,
			IReadOnlyCollection<ITrackedObject<TItem, TDiff>> trackedItems)
		{
			_source = source;
			_itemsSelector = itemsSelector;
			_matchingPredicate = matchingPredicate;
			_addedFactory = addedFactory;
			_removedFactory = removedFactory;
			_trackedItems = trackedItems;
		}

		public TDiff[] GetDifferences(TType target)
		{
			// Materialize target items once to avoid multiple enumerations
			var targetItemsList = _itemsSelector(target).ToArray();
			var matchedTargetIndices = new HashSet<int>();

			var differences = new List<TDiff>();

			// First pass: Match source trackers with target items
			foreach (var trackedItem in _trackedItems)
			{
				TItem? matchedItem = default;
				int matchedIndex = -1;

				// Find matching target item
				for (int i = 0; i < targetItemsList.Length; i++)
				{
					if (_matchingPredicate(trackedItem.Source, targetItemsList[i]))
					{
						matchedItem = targetItemsList[i];
						matchedIndex = i;
						break;
					}
				}

				if (matchedItem != null)
				{
					// TrackProperty matched target items to avoid duplicate processing
					matchedTargetIndices.Add(matchedIndex);
					differences.AddRange(trackedItem.Compare(matchedItem));
				}
				else if (_removedFactory != null)
				{
					differences.Add(_removedFactory(_source, target, trackedItem.Source));
				}
			}

			// Second pass: Find target items that weren't matched (additions)
			if (_addedFactory != null)
				for (int i = 0; i < targetItemsList.Length; i++)
					if (!matchedTargetIndices.Contains(i))
						differences.Add(_addedFactory(_source, target, targetItemsList[i]));

			return [.. differences];
		}
	}
}
