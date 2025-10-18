using ObjectTracker.TrackedObjects.TrackedItems;

namespace ObjectTracker.Trackers.ItemTrackers
{
	class PropertyTracker<TType, TDiff, TValue> : IItemTracker<TType, TDiff>
	{
		private readonly Func<TType, TValue?> _selector;
		private readonly Func<TType, TValue?, TValue?, TDiff> _differenceFactory;

		public PropertyTracker(
			Func<TType, TValue?> selector,
			Func<TType, TValue?, TValue?, TDiff> differenceFactory)
		{
			_selector = selector;
			_differenceFactory = differenceFactory;
		}

		public ITrackedItem<TType, TDiff> GetTrackedItem(TType source)
		{
			return new TrackedProperty<TType, TDiff, TValue>(
				source: source,
				selector: _selector,
				differenceFactory: _differenceFactory);
		}
	}
}
