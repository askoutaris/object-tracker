using ObjectTracker.TrackedObjects.TrackedItems;

namespace ObjectTracker.TrackedObjects
{
	public interface ITrackedObject<TType, TDiff>
	{
		TType Source { get; }

		IReadOnlyCollection<TDiff> Compare(TType target);
		IReadOnlyCollection<TDiff> GetDifferences();
	}

	class TrackedObject<TType, TDiff> : ITrackedObject<TType, TDiff>
	{
		private readonly IReadOnlyCollection<ITrackedItem<TType, TDiff>> _trackedItems;
		public TType Source { get; }

		public TrackedObject(TType source, IReadOnlyCollection<ITrackedItem<TType, TDiff>> trackedItems)
		{
			Source = source;
			_trackedItems = trackedItems;
		}

		public IReadOnlyCollection<TDiff> Compare(TType target)
		{
			return [.. _trackedItems.SelectMany(value => value.GetDifferences(target))];
		}

		public IReadOnlyCollection<TDiff> GetDifferences()
		{
			return [.. _trackedItems.SelectMany(value => value.GetDifferences(Source))];
		}
	}
}
