namespace ObjectTracker
{
	public interface ITracker<TType, TDiff>
	{
		Tracker<TType, TDiff> Track<TValue>(Func<TType, TValue> selector, Func<TValue?, TValue?, TDiff> differenceFactory);
		Tracker<TType, TDiff> TrackItems<TItem>(Func<TType, IEnumerable<TItem>> itemsSelector, Func<TItem, TItem, bool> matchingPredicate, Func<TType, TType, TItem, TDiff>? addedFactory = null, Func<TType, TType, TItem, TDiff>? removedFactory = null, Action<Tracker<TItem, TDiff>>? configureTracker = null);
		TDiff[] Compare(TType target);
	}

	public partial class Tracker<TType, TDiff> : ITracker<TType, TDiff>
	{
		private readonly List<ITrackedValue> _trackedValues;
		public TType Source { get; }

		protected Tracker(TType source)
		{
			_trackedValues = [];
			Source = source;
		}

		public static ITracker<TType, TDiff> CreateNew(TType source)
			=> new Tracker<TType, TDiff>(source);

		public Tracker<TType, TDiff> Track<TValue>(
			Func<TType, TValue> selector,
			Func<TValue?, TValue?, TDiff> differenceFactory)
		{
			_trackedValues.Add(new TrackedValue<TValue>(Source, selector, differenceFactory));
			return this;
		}

		public Tracker<TType, TDiff> TrackItems<TItem>(
			Func<TType, IEnumerable<TItem>> itemsSelector,
			Func<TItem, TItem, bool> matchingPredicate,
			Func<TType, TType, TItem, TDiff>? addedFactory = null,
			Func<TType, TType, TItem, TDiff>? removedFactory = null,
			Action<Tracker<TItem, TDiff>>? configureTracker = null)
		{
			_trackedValues.Add(new TrackCollection<TItem>(Source, itemsSelector, matchingPredicate, addedFactory, removedFactory, configureTracker));

			return this;
		}

		public TDiff[] Compare(TType target)
		{
			return _trackedValues
				.SelectMany(value => value.Compare(target))
				.ToArray();
		}
	}
}
