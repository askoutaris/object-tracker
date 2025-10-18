using ObjectTracker.TrackedObjects;
using ObjectTracker.TrackedObjects.TrackedItems;
using ObjectTracker.Trackers.ItemTrackers;

namespace ObjectTracker.Trackers
{
	public interface ITracker<TType, TDiff>
	{
		ITrackedObject<TType, TDiff> Track(TType source);
	}

	class Tracker<TType, TDiff> : ITracker<TType, TDiff>
	{
		private readonly IReadOnlyCollection<IItemTracker<TType, TDiff>> _itemTrackers;

		public Tracker(IReadOnlyCollection<IItemTracker<TType, TDiff>> itemTrackers)
		{
			_itemTrackers = itemTrackers;
		}

		public ITrackedObject<TType, TDiff> Track(TType source)
		{
			var trackedItems = GetTrackedItems(source);

			return new TrackedObject<TType, TDiff>(source, trackedItems);
		}

		private IReadOnlyCollection<ITrackedItem<TType, TDiff>> GetTrackedItems(TType source)
		{
			return [.. _itemTrackers.Select(value => value.GetTrackedItem(source))];
		}
	}
}
