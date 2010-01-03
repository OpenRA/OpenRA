
namespace OpenRa.Game.GameRules
{
	class UserSettings
	{
		// Debug settings
		public readonly bool UnitDebug = false;
		public readonly bool BuildingDebug = false;
		public readonly bool PathDebug = false;
		
		// Window settings
		public readonly int Width = 0;
		public readonly int Height = 0;
		public readonly bool Fullscreen = false;
		
		// Internal game settings
		public readonly int Timestep = 40;
		public readonly int SheetSize = 512;
		
		// External game settings
		public readonly bool UseAftermath = false;
		public readonly string NetworkHost = "";
		public readonly int NetworkPort = 0;
		public readonly string Map = "scm12ea.ini";
		public readonly int Player = 1;
		public readonly string Replay = "";
		
		// Gameplay options
		public readonly bool RepairRequiresConyard = true;

	}
}
