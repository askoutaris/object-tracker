using ObjectTracker;

namespace Workbench
{
	class Program
	{
		static void Main(string[] _)
		{
			Console.WriteLine("=== ObjectTracker Demo ===\n");

			// Create a person object that we'll track
			var person = new Person
			{
				Name = "John",
				Addresses =
				[
					new Address { Id = 1, City = "New York" },
					new Address { Id = 3, City = "Boston" },
					new Address { Id = 4, City = "Chicago" },
				]
			};

			Console.WriteLine("Initial state:");
			Console.WriteLine($"  Name: {person.Name}");
			foreach (var addr in person.Addresses)
				Console.WriteLine($"    Address {addr.Id}: {addr.City}");

			Console.WriteLine("\n--- Creating tracker (takes snapshot of current state) ---\n");

			// Create tracker - this captures the current state as the baseline
			var tracker = Tracker<Person, IDifference>.CreateNew(person)
				// Track the Name property
				.Track(
					selector: p => p.Name,
					differenceFactory: (oldName, newName) =>
						new PropertyChangeDifference("Name", oldName, newName))

				// Track Name length with custom difference type
				.Track(
					selector: p => p.Name,
					differenceFactory: (oldName, newName) =>
					{
						if (oldName != null && newName != null && newName.Length > oldName.Length)
							return new LongerNameDifference(oldName, newName);
						return null!; // Return null to skip this difference if condition not met
					})

				// Track collection of addresses
				.TrackItems(
					itemsSelector: p => p.Addresses,
					matchingPredicate: (addr1, addr2) => addr1.Id == addr2.Id,
					addedFactory: (src, tgt, addedAddress) =>
						new GenericDifference($"Address added - Id: {addedAddress.Id}, City: {addedAddress.City}"),
					removedFactory: (src, tgt, removedAddress) =>
						new GenericDifference($"Address removed - Id: {removedAddress.Id}, City: {removedAddress.City}"),
					configureTracker: addressTracker =>
					{
						// For each matched address, track the City property
						addressTracker.Track(
							selector: a => a.City,
							differenceFactory: (oldCity, newCity) =>
								new GenericDifference($"Address {addressTracker.Source.Id}: City changed from \"{oldCity}\" to \"{newCity}\""));
					});

			Console.WriteLine("--- Making changes to the person ---\n");

			// Now modify the same object
			person.Name = "John Doe";
			person.Addresses =
			[
				new Address { Id = 2, City = "Seattle" },     // Added (new ID)
				new Address { Id = 3, City = "Cambridge" },   // Modified (city changed)
				new Address { Id = 4, City = "Chicago" },     // Unchanged
				// Address with Id 1 was removed
			];

			Console.WriteLine("Modified state:");
			Console.WriteLine($"  Name: {person.Name}");
			foreach (var addr in person.Addresses)
				Console.WriteLine($"    Address {addr.Id}: {addr.City}");

			Console.WriteLine("\n--- Comparing against tracked snapshot ---\n");

			// Compare the current state against the original snapshot
			IDifference[] differences = tracker.Compare(person);

			Console.WriteLine($"Found {differences.Length} difference(s):\n");

			foreach (var diff in differences.Where(d => d != null))
			{
				Console.WriteLine($"  • {diff}");
			}

			Console.WriteLine("\n--- Making more changes ---\n");

			// Change the person again
			person.Name = "Jane";
			person.Addresses =
			[
				new Address { Id = 1, City = "Los Angeles" },
				new Address { Id = 3, City = "Boston" },
				new Address { Id = 4, City = "Chicago" },
			];

			Console.WriteLine("New state:");
			Console.WriteLine($"  Name: {person.Name}");
			foreach (var addr in person.Addresses)
				Console.WriteLine($"    Address {addr.Id}: {addr.City}");

			Console.WriteLine("\n--- Comparing again (still against original snapshot) ---\n");

			// The tracker always compares against the original snapshot from when it was created
			var moreDifferences = tracker.Compare(person);

			Console.WriteLine($"Found {moreDifferences.Length} difference(s):\n");

			foreach (var diff in moreDifferences.Where(d => d != null))
			{
				Console.WriteLine($"  • {diff}");
			}

			Console.WriteLine("\n--- Demo Complete ---");
			Console.WriteLine("\nPress Enter to exit...");
			Console.ReadLine();
		}
	}
}
