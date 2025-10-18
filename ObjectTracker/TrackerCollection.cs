namespace ObjectTracker
{
	public partial class Tracker<TType, TDiff> : ITracker<TType, TDiff>
	{
		class TrackCollection<TItem> : ITrackedValue
		{
			private readonly TType _source;

			private readonly Func<TType, IEnumerable<TItem>> _itemsSelector;
			private readonly Func<TItem, TItem, bool> _matchingPredicate;
			private readonly Func<TType, TType, TItem, TDiff>? _addedFactory;
			private readonly Func<TType, TType, TItem, TDiff>? _removedFactory;
			private readonly List<Tracker<TItem, TDiff>> _itemTrackers;

			public TrackCollection(
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
				var targetItems = _itemsSelector(target);

				var differences = new List<TDiff>();
				foreach (var tracker in _itemTrackers)
				{
					TItem? matchedItem = targetItems.SingleOrDefault(targetItem => _matchingPredicate(tracker.Source, targetItem));

					if (matchedItem != null)
						differences.AddRange(tracker.Compare(matchedItem));
					else if (matchedItem == null && _removedFactory != null)
						differences.Add(_removedFactory(_source, target, tracker.Source));
				}

				foreach (var targetItem in targetItems)
				{
					var tracker = _itemTrackers.SingleOrDefault(tracker => _matchingPredicate(targetItem, tracker.Source));

					if (tracker == null && _addedFactory != null)
						differences.Add(_addedFactory(_source, target, targetItem));
				}

				return [.. differences];
			}
		}
	}
}
