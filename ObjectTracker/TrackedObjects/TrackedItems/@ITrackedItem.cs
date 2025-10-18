namespace ObjectTracker.TrackedObjects.TrackedItems
{
	interface ITrackedItem<TType, TDiff>
	{
		TDiff[] GetDifferences(TType target);
	}
}
