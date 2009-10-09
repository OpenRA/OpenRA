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

		// TODO: load rules from the map, where appropriate.
		public static void LoadRules()
		{
			UnitInfo = new UnitInfo( new IniFile( FileSystem.Open( "rules.ini" ) ) );
		}
	}
}
