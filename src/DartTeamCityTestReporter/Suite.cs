namespace DartTeamCityTestReporter
{
	public class Suite
	{
		private static int _order = 1;

		public Suite()
		{
			Order = _order++;
		}

		public int Order { get; set; }
		public int GroupId { get; set; }
		public string Name { get; set; }
		public int TestsRemaining { get; set; }
	}
}