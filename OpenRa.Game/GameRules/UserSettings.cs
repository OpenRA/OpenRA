
namespace OpenRa.Game.GameRules
{
	class UserSettings
	{
		public readonly bool UnitDebug = false;
		public readonly bool BuildingDebug = false;
		public readonly bool PathDebug = false;
		public readonly int Timestep = 40;
		public readonly string Replay = "";
		public readonly bool UseAftermath = false;
		public readonly string NetworkHost = "";
		public readonly int NetworkPort = 0;
		public readonly int SheetSize = 512;
		public readonly bool Fullscreen = false;
		public readonly string Map = "scm12ea.ini";
		public readonly int Player = 1;
		public readonly int Width = 0;
		public readonly int Height = 0;
	}
}
