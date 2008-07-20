using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	static class Rules
	{
		static readonly Dictionary<string, UnitInfo> unitInfos = new Dictionary<string, UnitInfo>();

		static Rules()
		{
            var rulesIni = SharedResources.Rules;

			foreach (string line in Util.ReadAllLines(FileSystem.Open("units.txt")))
			{
				string unit = line.Substring( 0, line.IndexOf( ',' ) ).ToUpperInvariant();
				IniSection section = rulesIni.GetSection( unit );
				if (section == null)
				{
					Log.Write("rules.ini doesnt contain entry for unit \"{0}\"", unit);
					continue;
				}
				unitInfos.Add( unit, new UnitInfo( unit, section ) );
			}
		}

		public static UnitInfo UnitInfo( string name )
		{
			return unitInfos[ name.ToUpperInvariant() ];
		}
	}
}
