using ObjectTracker.TrackedObjects.TrackedItems;

namespace ObjectTracker.TrackedObjects
{
	/// <summary>
	/// Immutable snapshot of an object's tracked state with comparison methods.
	/// Created by <see cref="ObjectTracker.Trackers.Tracker{TType, TDiff}.Track"/> to capture state at a point in time.
	/// </summary>
	/// <typeparam name="TType">Type of object being tracked.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	public interface ITrackedObject<TType, TDiff>
	{
		/// <summary>
		/// The original object that was tracked. Reference is preserved for comparisons.
		/// </summary>
		TType Source { get; }

		/// <summary>
		/// Compares the captured snapshot against a different target object.
		/// </summary>
		/// <param name="target">Object to compare against the snapshot.</param>
		/// <returns>Collection of all differences detected across all tracked items.</returns>
		IReadOnlyCollection<TDiff> Compare(TType target);

		/// <summary>
		/// Detects mutations by comparing the source object against its captured snapshot.
		/// Useful for detecting changes made to the original object after tracking.
		/// </summary>
		/// <returns>Collection of all differences detected between current state and snapshot.</returns>
		IReadOnlyCollection<TDiff> GetDifferences();
	}

	/// <summary>
	/// Immutable snapshot containing captured state from all tracked items.
	/// Holds <see cref="ITrackedItem{TType, TDiff}"/> instances that contain property/collection snapshots.
	/// </summary>
	/// <typeparam name="TType">Type of object being tracked.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	class TrackedObject<TType, TDiff> : ITrackedObject<TType, TDiff>
	{
		private readonly IReadOnlyCollection<ITrackedItem<TType, TDiff>> _trackedItems;

		/// <inheritdoc/>
		public TType Source { get; }

		/// <summary>
		/// Initializes a tracked object with source reference and captured snapshots.
		/// </summary>
		/// <param name="source">The object that was tracked.</param>
		/// <param name="trackedItems">Collection of tracked items containing captured state.</param>
		public TrackedObject(TType source, IReadOnlyCollection<ITrackedItem<TType, TDiff>> trackedItems)
		{
			Source = source;
			_trackedItems = trackedItems;
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<TDiff> Compare(TType target)
		{
			return [.. _trackedItems.SelectMany(value => value.GetDifferences(target))];
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<TDiff> GetDifferences()
		{
			return [.. _trackedItems.SelectMany(value => value.GetDifferences(Source))];
		}
	}
}
