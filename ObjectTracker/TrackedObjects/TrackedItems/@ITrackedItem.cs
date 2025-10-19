namespace ObjectTracker.TrackedObjects.TrackedItems
{
	/// <summary>
	/// Captured state snapshot with comparison logic to detect differences against a target.
	/// Part of the state layer - holds immutable snapshots from tracking time.
	/// </summary>
	/// <typeparam name="TType">Type of object being compared.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	interface ITrackedItem<TType, TDiff>
	{
		/// <summary>
		/// Compares the captured snapshot against a target object to detect differences.
		/// </summary>
		/// <param name="target">Object to compare against the snapshot.</param>
		/// <returns>Array of differences detected between snapshot and target.</returns>
		TDiff[] GetDifferences(TType target);
	}
}
