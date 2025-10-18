namespace ObjectTracker
{
	public partial class Tracker<TType, TDiff>
	{
		class TrackedProperty<TValue> : ITrackedItem
		{
			private readonly TValue? _sourceValue;
			public Func<TType, TValue?> Selector { get; }
			public Func<TValue?, TValue?, TDiff> DifferenceFactory { get; }

			public TrackedProperty(TType source, Func<TType, TValue?> selector, Func<TValue?, TValue?, TDiff> differenceFactory)
			{
				_sourceValue = selector(source);
				Selector = selector;
				DifferenceFactory = differenceFactory;
			}

			public TDiff[] Compare(TType target)
			{
				var targetValue = Selector(target);

				if (_sourceValue is not null && targetValue is not null && _sourceValue.Equals(targetValue) == false)
					return [DifferenceFactory(_sourceValue, targetValue)];
				else if (_sourceValue is null && targetValue is null)
					return [];
				else if (_sourceValue is null || targetValue is null)
					return [DifferenceFactory(_sourceValue, targetValue)];
				else
					return [];
			}
		}
	}
}
