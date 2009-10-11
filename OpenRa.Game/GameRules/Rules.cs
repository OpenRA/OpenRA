using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
{
	static class Rules
	{
		public static UnitInfo UnitInfo;
		public static Footprint Footprint;

		// TODO: load rules from the map, where appropriate.
		public static void LoadRules()
		{
			UnitInfo = new UnitInfo( new IniFile( FileSystem.Open( "rules.ini" ) ) );
			Footprint = new Footprint(FileSystem.Open("footprint.txt"));
		}
	}
}
