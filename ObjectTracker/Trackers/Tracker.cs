using ObjectTracker.TrackedObjects;
using ObjectTracker.TrackedObjects.TrackedItems;
using ObjectTracker.Trackers.ItemTrackers;

namespace ObjectTracker.Trackers
{
	/// <summary>
	/// Reusable tracker that captures object state snapshots.
	/// Configured once via <see cref="TrackerBuilder{TType, TDiff}"/>, then used to track multiple objects efficiently.
	/// </summary>
	/// <typeparam name="TType">Type of object to track.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	public interface ITracker<TType, TDiff>
	{
		/// <summary>
		/// Captures a snapshot of the source object's tracked state.
		/// </summary>
		/// <param name="source">Object to capture snapshot from.</param>
		/// <returns>Tracked object containing immutable state snapshot and comparison logic.</returns>
		ITrackedObject<TType, TDiff> Track(TType source);
	}

	/// <summary>
	/// Reusable configuration template containing tracking rules.
	/// Holds <see cref="IItemTracker{TType, TDiff}"/> instances that define what to track.
	/// Stateless and memory-efficient - can snapshot thousands of objects without rebuilding configuration.
	/// </summary>
	/// <typeparam name="TType">Type of object to track.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	class Tracker<TType, TDiff> : ITracker<TType, TDiff>
	{
		private readonly IReadOnlyCollection<IItemTracker<TType, TDiff>> _itemTrackers;

		/// <summary>
		/// Initializes a tracker with configured tracking rules.
		/// </summary>
		/// <param name="itemTrackers">Collection of item trackers defining what to track.</param>
		public Tracker(IReadOnlyCollection<IItemTracker<TType, TDiff>> itemTrackers)
		{
			_itemTrackers = itemTrackers;
		}

		/// <inheritdoc/>
		public ITrackedObject<TType, TDiff> Track(TType source)
		{
			var trackedItems = GetTrackedItems(source);

			return new TrackedObject<TType, TDiff>(source, trackedItems);
		}

		/// <summary>
		/// Creates tracked items by applying all configured item trackers to capture state snapshots.
		/// </summary>
		/// <param name="source">Object to capture snapshots from.</param>
		/// <returns>Collection of tracked items holding captured state.</returns>
		private IReadOnlyCollection<ITrackedItem<TType, TDiff>> GetTrackedItems(TType source)
		{
			return [.. _itemTrackers.Select(value => value.GetTrackedItem(source))];
		}
	}
}
