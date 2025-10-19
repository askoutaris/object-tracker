using ObjectTracker.TrackedObjects.TrackedItems;

namespace ObjectTracker.Trackers.ItemTrackers
{
	/// <summary>
	/// Reusable configuration for tracking a single property.
	/// Holds the selector and difference factory, creates <see cref="TrackedProperty{TType, TDiff, TValue}"/> instances to capture snapshots.
	/// Stateless and reusable across multiple objects.
	/// </summary>
	/// <typeparam name="TType">Type of object containing the property.</typeparam>
	/// <typeparam name="TDiff">Type representing a difference between values.</typeparam>
	/// <typeparam name="TValue">Type of the property value.</typeparam>
	class PropertyTracker<TType, TDiff, TValue> : IItemTracker<TType, TDiff>
	{
		private readonly Func<TType, TValue?> _selector;
		private readonly Func<TType, TValue?, TValue?, TDiff> _differenceFactory;

		/// <summary>
		/// Initializes a property tracker with selector and difference factory.
		/// </summary>
		/// <param name="selector">Function to extract the property value from an object.</param>
		/// <param name="differenceFactory">Function to create difference objects when values differ.</param>
		public PropertyTracker(
			Func<TType, TValue?> selector,
			Func<TType, TValue?, TValue?, TDiff> differenceFactory)
		{
			_selector = selector;
			_differenceFactory = differenceFactory;
		}

		/// <inheritdoc/>
		public ITrackedItem<TType, TDiff> GetTrackedItem(TType source)
		{
			return new TrackedProperty<TType, TDiff, TValue>(
				source: source,
				selector: _selector,
				differenceFactory: _differenceFactory);
		}
	}
}
