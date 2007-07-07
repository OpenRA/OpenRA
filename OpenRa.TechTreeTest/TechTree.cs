using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.IO;
using System.Text.RegularExpressions;

namespace OpenRa.TechTreeTest
{
	class TechTree
	{
		Dictionary<string, Building> buildings = new Dictionary<string, Building>();
		public ICollection<string> built = new List<string>();
		readonly BuildingRace currentRace;

		public TechTree(BuildingRace race)
		{
			this.currentRace = race;
			LoadBuildings();
			LoadRules();

			built.Add("FACT");
			CheckAll();
		}

		void LoadRules()
		{
			IniFile rulesFile;
			rulesFile = new IniFile(File.OpenRead("../../../rules.ini"));
			foreach (string key in buildings.Keys)
			{
				IniSection section = rulesFile.GetSection(key);
				Building b = buildings[key];
				string s = section.GetValue("Prerequisite", "").ToUpper();
				b.Prerequisites = s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				b.TechLevel = int.Parse(section.GetValue("TechLevel", "-1"));
				s = section.GetValue("Owner", "");
				
				if (string.IsNullOrEmpty(s))
				{
					b.Owner = BuildingRace.None;
					continue;
				}
				
				if (s.Equals("Both", StringComparison.InvariantCultureIgnoreCase))
				{
					b.Owner = BuildingRace.Allies | BuildingRace.Soviet;
					continue;
				}
				
				string[] frags = s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (frags.Length > 1)
					b.Owner = BuildingRace.Allies | BuildingRace.Soviet;
				else
					b.Owner = (BuildingRace)Enum.Parse(typeof(BuildingRace), frags[0], true);
			}
		}

		void LoadBuildings()
		{
			foreach (string line in File.ReadAllLines("../../../buildings.txt"))
			{
				Regex pattern = new Regex(@"^(\w+),([\w ]+)$");
				Match m = pattern.Match(line);
				if (!m.Success) continue;
				buildings.Add(m.Groups[1].Value, new Building(m.Groups[1].Value, m.Groups[2].Value));
			}
		}

		public bool Build(string key)
		{
			Building b = buildings[key];
			if (!b.Buildable) return false;
			built.Add(key);
			CheckAll();
			return true;
		}

		public bool Unbuild(string key)
		{
			Building b = buildings[key];
			if (!built.Contains(key)) return false;
			built.Remove(key);
			CheckAll();
			return true;
		}

		void CheckAll()
		{
			foreach (Building building in buildings.Values)
				building.CheckPrerequisites(built, currentRace);
		}

		public IEnumerable<Building> BuildableItems
		{
			get
			{
				foreach (Building b in buildings.Values)
					if (b.Buildable)
						yield return b;
			}
		}
	}
}
