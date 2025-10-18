namespace Workbench
{
	public class Person
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int Age { get; set; }
		public List<Address> Addresses { get; set; }
	}

	public class Address
	{
		public int Id { get; set; }
		public string City { get; set; }
	}
}
