using ObjectTracker.TrackedObjects.TrackedItems;

namespace ObjectTracker.Trackers.ItemTrackers
{
	interface IItemTracker<TType, TDiff>
	{
		ITrackedItem<TType, TDiff> GetTrackedItem(TType source);
	}
}
