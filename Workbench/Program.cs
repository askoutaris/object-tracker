using ObjectTracker.Builders;

namespace Workbench
{
	class Program
	{
		static void Main(string[] _)
		{
			var trackerBuilder = new TrackerBuilder<Person, IDifference>()
				.TrackProperty(
					selector: person => person.Name,
					differenceFactory: (person, oldValue, newValue) => new GenericChange($"Name changed from {oldValue} to {newValue} for person {person.Id}"))
				.TrackProperty(
					selector: person => person.Age,
					differenceFactory: (person, oldValue, newValue) => new GenericChange($"Age changed from {oldValue} to {newValue} for person {person.Id}"))
				.TrackCollection(
					itemsSelector: x => x.Addresses,
					matchingPredicate: (sourceAddress, targetAddress) => sourceAddress.Id == targetAddress.Id,
					addedFactory: (sourcePerson, targetPerson, addedAddress) => new GenericChange($"Address added {addedAddress.City}"),
					removedFactory: (sourcePerson, targetPerson, removedAddress) => new GenericChange($"Address removed {removedAddress.City}"),
					configureItemTracker: itemTracker => itemTracker
						.TrackProperty(
							selector: address => address.City,
							differenceFactory: (address, oldValue, newValue) => new GenericChange($"Name changed from {oldValue} to {newValue} for address {address.Id}"))
				);

			var tracker = trackerBuilder.Build();

			var person = new Person
			{
				Id = 1,
				Age = 35,
				Name = "John",
				Addresses = [
					new Address { Id =1 , City = "New York" },
					new Address { Id =2 , City = "London" },
					new Address { Id =3 , City = "Paris" },
				]
			};

			var trackedPerson = tracker.Track(person);

			person.Name = "David";
			person.Age = 36;
			person.Addresses[0].City = "Madrid";
			person.Addresses.Add(new Address { Id = 4, City = "Liverpool" });
			person.Addresses.RemoveAll(x => x.Id == 3);

			var differences = trackedPerson.GetDifferences();

			foreach (var change in differences)
				Console.WriteLine(change.ToString());

			Console.ReadLine();
		}
	}
}
