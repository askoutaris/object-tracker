using ObjectTracker.Trackers;
using ObjectTracker.Trackers.ItemTrackers;

namespace ObjectTracker.Builders
{
	public interface ITrackerBuilder<TType, TDiff>
	{
		ITracker<TType, TDiff> Build();
		ITrackerBuilder<TType, TDiff> TrackProperty<TValue>(Func<TType, TValue?> selector, Func<TType, TValue?, TValue?, TDiff> differenceFactory);
		ITrackerBuilder<TType, TDiff> TrackCollection<TItem>(Func<TType, IEnumerable<TItem>> itemsSelector, Func<TItem, TItem, bool> matchingPredicate, Func<TType, TType, TItem, TDiff>? addedFactory, Func<TType, TType, TItem, TDiff>? removedFactory, Func<ITrackerBuilder<TItem, TDiff>, ITrackerBuilder<TItem, TDiff>> configureItemTracker);
	}

	public class TrackerBuilder<TType, TDiff> : ITrackerBuilder<TType, TDiff>
	{
		private readonly List<IItemTracker<TType, TDiff>> _itemTrackers;

		public TrackerBuilder()
		{
			_itemTrackers = [];
		}

		public ITrackerBuilder<TType, TDiff> TrackProperty<TValue>(Func<TType, TValue?> selector, Func<TType, TValue?, TValue?, TDiff> differenceFactory)
		{
			var tracker = new PropertyTracker<TType, TDiff, TValue>(
				selector: selector,
				differenceFactory: differenceFactory);

			_itemTrackers.Add(tracker);

			return this;
		}

		public ITrackerBuilder<TType, TDiff> TrackCollection<TItem>(Func<TType, IEnumerable<TItem>> itemsSelector, Func<TItem, TItem, bool> matchingPredicate, Func<TType, TType, TItem, TDiff>? addedFactory, Func<TType, TType, TItem, TDiff>? removedFactory, Func<ITrackerBuilder<TItem, TDiff>, ITrackerBuilder<TItem, TDiff>> configureItemTracker)
		{
			var initialItemTrackerBuilder = new TrackerBuilder<TItem, TDiff>();

			var configuredItemTrackerBuilder = configureItemTracker(initialItemTrackerBuilder);

			var tracker = new CollectionTracker<TType, TDiff, TItem>(
				itemsSelector: itemsSelector,
				matchingPredicate: matchingPredicate,
				addedFactory: addedFactory,
				removedFactory: removedFactory,
				itemTracker: configuredItemTrackerBuilder.Build());

			_itemTrackers.Add(tracker);

			return this;

		}

		public ITracker<TType, TDiff> Build()
		{
			return new Tracker<TType, TDiff>(_itemTrackers);
		}
	}
}
