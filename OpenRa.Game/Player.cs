
namespace OpenRa.Game
{
	class Player
	{
		public int Palette;
		public string PlayerName;
		public TechTree.TechTree TechTree = new OpenRa.TechTree.TechTree();

		public Player( int palette, string playerName, OpenRa.TechTree.Race race )
		{
			this.Palette = palette;
			this.PlayerName = playerName;
			TechTree.CurrentRace = race;
		}

		public float GetSiloFullness()
		{
			return 0.5f;		/* todo: work this out the same way as RA */
		}
	}
}
