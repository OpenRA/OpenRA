using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.TechTreeTest
{
	class Building
	{
		readonly string friendlyName;

		public string FriendlyName
		{
			get { return friendlyName; }
		}

		string[] prerequisites;

		public string[] Prerequisites
		{
			get { return prerequisites; }
			set { prerequisites = value; }
		}

		int techLevel;

		public int TechLevel
		{
			get { return techLevel; }
			set { techLevel = value; }
		}

		public Building(string friendlyName)
		{
			this.friendlyName = friendlyName;
		}

		public bool ShouldMakeBuildable(IEnumerable<string> buildings)
		{
			List<string> p = new List<string>(prerequisites);
			foreach (string b in buildings)
				p.Remove(b);

			return p.Count == 0;
		}

		public bool ShouldMakeUnbuildable(IEnumerable<string> buildings)
		{
			List<string> p = new List<string>(prerequisites);
			foreach (string b in buildings)
				p.Remove(b);

			return p.Count == prerequisites.Length;
		}

		bool buildable = false;
		public bool Buildable { get { return buildable; } }

		public void CheckPrerequisites(IEnumerable<string> buildings)
		{
			if (buildable && ShouldMakeUnbuildable(buildings))
				buildable = false;
			else if (!buildable && ShouldMakeBuildable(buildings))
				buildable = true;
		}
	}
}
