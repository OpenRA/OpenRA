using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
{
	static class Rules
	{
		public static UnitInfoLoader UnitInfo;
		public static WeaponInfoLoader WeaponInfo;
		public static Footprint Footprint;

		// TODO: load rules from the map, where appropriate.
		public static void LoadRules()
		{
			var rulesIni = new IniFile( FileSystem.Open( "rules.ini" ) );
			UnitInfo = new UnitInfoLoader( rulesIni );
			WeaponInfo = new WeaponInfoLoader( rulesIni );
			Footprint = new Footprint(FileSystem.Open("footprint.txt"));
		}
	}
}
