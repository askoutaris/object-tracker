namespace ObjectTracker.TrackedObjects.TrackedItems
{
	/// <summary>
	/// Captured property value snapshot with comparison logic.
	/// Stores the property value at tracking time and compares it against target values using .Equals().
	/// Holds references to selector and difference factory for comparison operations.
	/// </summary>
	/// <typeparam name="TType">Type of object containing the property.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	/// <typeparam name="TValue">Type of the property value.</typeparam>
	class TrackedProperty<TType, TDiff, TValue> : ITrackedItem<TType, TDiff>
	{
		private readonly TValue? _sourceValue;

		/// <summary>
		/// Function to extract the property value from an object. Used during comparison.
		/// </summary>
		public Func<TType, TValue?> Selector { get; }

		/// <summary>
		/// Function to create difference objects when values differ. Receives target object and both values.
		/// </summary>
		public Func<TType, TValue?, TValue?, TDiff> DifferenceFactory { get; }

		/// <summary>
		/// Initializes a tracked property by capturing the property value from the source object.
		/// </summary>
		/// <param name="source">Object to capture the property value from.</param>
		/// <param name="selector">Function to extract the property value.</param>
		/// <param name="differenceFactory">Function to create difference objects when values differ.</param>
		public TrackedProperty(
			TType source,
			Func<TType, TValue?> selector,
			Func<TType, TValue?, TValue?, TDiff> differenceFactory)
		{
			_sourceValue = selector(source);
			Selector = selector;
			DifferenceFactory = differenceFactory;
		}

		/// <summary>
		/// Compares the captured property value against the target object's property value.
		/// Uses .Equals() for comparison. Handles null values - reports difference if one is null and other isn't.
		/// </summary>
		/// <param name="target">Object to extract and compare property value from.</param>
		/// <returns>Single-element array with difference if values differ, empty array if equal.</returns>
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
