namespace ObjectTracker
{
	public partial class Tracker<TType, TDiff> : ITracker<TType, TDiff>
	{
		class TrackedCollection<TItem> : ITrackedItem
		{
			private readonly TType _source;

			private readonly Func<TType, IEnumerable<TItem>> _itemsSelector;
			private readonly Func<TItem, TItem, bool> _matchingPredicate;
			private readonly Func<TType, TType, TItem, TDiff>? _addedFactory;
			private readonly Func<TType, TType, TItem, TDiff>? _removedFactory;
			private readonly List<Tracker<TItem, TDiff>> _itemTrackers;

			public TrackedCollection(
				TType source,
				Func<TType, IEnumerable<TItem>> itemsSelector,
				Func<TItem, TItem, bool> matchingPredicate,
				Func<TType, TType, TItem, TDiff>? addedFactory,
				Func<TType, TType, TItem, TDiff>? removedFactory,
				Action<Tracker<TItem, TDiff>>? configureTracker = null)
			{
				_source = source;
				_itemsSelector = itemsSelector;
				_matchingPredicate = matchingPredicate;
				_addedFactory = addedFactory;
				_removedFactory = removedFactory;

				_itemTrackers = [];
				foreach (var item in itemsSelector(source))
				{
					var itemTracker = new Tracker<TItem, TDiff>(item);

					if (configureTracker is not null)
						configureTracker(itemTracker);

					_itemTrackers.Add(itemTracker);
				}
			}

			public TDiff[] Compare(TType target)
			{
				// Materialize target items once to avoid multiple enumerations
				var targetItemsList = _itemsSelector(target).ToArray();
				var matchedTargetIndices = new HashSet<int>();

				var differences = new List<TDiff>();

				// First pass: Match source trackers with target items
				foreach (var tracker in _itemTrackers)
				{
					TItem? matchedItem = default;
					int matchedIndex = -1;

					// Find matching target item
					for (int i = 0; i < targetItemsList.Length; i++)
					{
						if (_matchingPredicate(tracker.Source, targetItemsList[i]))
						{
							matchedItem = targetItemsList[i];
							matchedIndex = i;
							break;
						}
					}

					if (matchedItem != null)
					{
						// Track matched target items to avoid duplicate processing
						matchedTargetIndices.Add(matchedIndex);
						differences.AddRange(tracker.Compare(matchedItem));
					}
					else if (_removedFactory != null)
					{
						differences.Add(_removedFactory(_source, target, tracker.Source));
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
}
