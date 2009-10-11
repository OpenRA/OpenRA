using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.GameRules
{
	class Footprint
	{
		Dictionary<string, string[]> buildingFootprints;

		public string[] GetFootprint(string name)
		{
			string[] val;
			if (!buildingFootprints.TryGetValue(name, out val))
				buildingFootprints.TryGetValue("*", out val);
			return val;
		}

		public Footprint(Stream s)
		{
			var lines = Util.ReadAllLines(s).Where(a => !a.StartsWith("#"));

			Func<string,string[]> words = 
				b => b.Split( new[] { ' ', '\t' }, 
					StringSplitOptions.RemoveEmptyEntries );

			var buildings = lines
				.Select(a => a.Split(':'))
				.SelectMany(a => words(a[1])
					.Select( b => new { Name=b, Pat=words(a[0]) } ));

			buildingFootprints = buildings
				.ToDictionary(a => a.Name, a => a.Pat);
		}
	}
}
