using ObjectTracker.TrackedObjects.TrackedItems;

namespace ObjectTracker.Trackers.ItemTrackers
{
	/// <summary>
	/// Reusable configuration template that creates tracked items from source objects.
	/// Part of the configuration layer - stateless and reusable across multiple objects.
	/// </summary>
	/// <typeparam name="TType">Type of object to track.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	interface IItemTracker<TType, TDiff>
	{
		/// <summary>
		/// Creates a tracked item by capturing a snapshot of the source object's state.
		/// </summary>
		/// <param name="source">Object to capture snapshot from.</param>
		/// <returns>Tracked item containing the captured state.</returns>
		ITrackedItem<TType, TDiff> GetTrackedItem(TType source);
	}
}
