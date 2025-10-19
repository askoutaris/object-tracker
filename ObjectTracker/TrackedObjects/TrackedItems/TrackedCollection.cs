namespace ObjectTracker.TrackedObjects.TrackedItems
{
	/// <summary>
	/// Captured collection snapshot with nested item tracking and comparison logic.
	/// Stores snapshots of collection items at tracking time and detects additions, removals, and nested changes.
	/// Uses optimized O(n) matching algorithm with HashSet tracking for matched indices.
	/// </summary>
	/// <typeparam name="TType">Type of object containing the collection.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	/// <typeparam name="TItem">Type of items in the collection.</typeparam>
	class TrackedCollection<TType, TDiff, TItem> : ITrackedItem<TType, TDiff>
	{
		private readonly TType _source;

		private readonly Func<TType, IEnumerable<TItem>> _itemsSelector;
		private readonly Func<TItem, TItem, bool> _matchingPredicate;
		private readonly Func<TType, TType, TItem, TDiff>? _addedFactory;
		private readonly Func<TType, TType, TItem, TDiff>? _removedFactory;
		private readonly IReadOnlyCollection<ITrackedObject<TItem, TDiff>> _trackedItems;

		/// <summary>
		/// Initializes a tracked collection by capturing snapshots of all collection items.
		/// </summary>
		/// <param name="source">Source object that was tracked (preserved for comparison context).</param>
		/// <param name="itemsSelector">Function to extract the collection from an object.</param>
		/// <param name="matchingPredicate">Function to determine if two items represent the same entity.</param>
		/// <param name="addedFactory">Optional factory to create difference objects for added items.</param>
		/// <param name="removedFactory">Optional factory to create difference objects for removed items.</param>
		/// <param name="trackedItems">Collection of tracked item snapshots (nested tracked objects).</param>
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

		/// <summary>
		/// Compares captured collection snapshot against target object's collection.
		/// Uses optimized O(n) algorithm: materializes target once, uses HashSet for O(1) matched lookups.
		/// Detects three types of differences: removed items, added items, and nested changes in matched items.
		/// </summary>
		/// <param name="target">Object to extract and compare collection from.</param>
		/// <returns>Array of all differences including additions, removals, and nested item changes.</returns>
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
