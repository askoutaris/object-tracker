namespace ObjectTracker.TrackedObjects.TrackedItems
{
	class TrackedProperty<TType, TDiff, TValue> : ITrackedItem<TType, TDiff>
	{
		private readonly TValue? _sourceValue;

		public Func<TType, TValue?> Selector { get; }
		public Func<TType, TValue?, TValue?, TDiff> DifferenceFactory { get; }

		public TrackedProperty(
			TType source,
			Func<TType, TValue?> selector,
			Func<TType, TValue?, TValue?, TDiff> differenceFactory)
		{
			_sourceValue = selector(source);
			Selector = selector;
			DifferenceFactory = differenceFactory;
		}

		public TDiff[] GetDifferences(TType target)
		{
			var targetValue = Selector(target);

			if (_sourceValue is not null && targetValue is not null && _sourceValue.Equals(targetValue) == false)
				return [DifferenceFactory(target, _sourceValue, targetValue)];
			else if (_sourceValue is null && targetValue is null)
				return [];
			else if (_sourceValue is null || targetValue is null)
				return [DifferenceFactory(target, _sourceValue, targetValue)];
			else
				return [];
		}
	}
}
