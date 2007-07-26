using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace OpenRa.FileFormats
{
	public class Walkability
	{
		Dictionary<string, Dictionary<int, int>> walkability =
			new Dictionary<string, Dictionary<int, int>>();

		public Walkability()
		{
			IniFile file = new IniFile( FileSystem.Open( "templates.ini" ) );
			Regex pattern = new Regex(@"tiletype(\d+)");

			foreach (IniSection section in file.Sections)
			{
				string name = section.GetValue("Name", null).ToLowerInvariant();

				Dictionary<int, int> tileWalkability = new Dictionary<int, int>();
				foreach (KeyValuePair<string, string> p in section)
				{
					Match m = pattern.Match(p.Key);
					if (m != null && m.Success)
						tileWalkability.Add(int.Parse(m.Groups[1].Value), int.Parse(p.Value));
				}

				walkability[name] = tileWalkability;
			}
		}

		public Dictionary<int, int> GetWalkability(string terrainName)
		{
			return walkability[terrainName];
		}
	}
}
