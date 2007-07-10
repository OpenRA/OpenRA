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
		Dictionary<string, IRAUnit> units = new Dictionary<string, IRAUnit>();
		public ICollection<string> built = new List<string>();
		readonly Race currentRace;

		public TechTree(Race race)
		{
			this.currentRace = race;
			LoadBuildings();
			LoadUnits();
			LoadRules();

			built.Add("FACT");
			CheckAll();
		}

		void LoadRules()
		{
			IniFile rulesFile;
			rulesFile = new IniFile(File.OpenRead("../../../rules.ini"));
			foreach (string key in units.Keys)
			{
				IniSection section = rulesFile.GetSection(key);
				IRAUnit b = units[key];
				string s = section.GetValue("Prerequisite", "").ToUpper();
				b.Prerequisites = s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				b.TechLevel = int.Parse(section.GetValue("TechLevel", "-1"));
				s = section.GetValue("Owner", "");
				
				if (string.IsNullOrEmpty(s))
				{
					s = section.GetValue("DoubleOwned", "No");
					if (s.Equals("Yes", StringComparison.InvariantCultureIgnoreCase))
						b.Owner = Race.Allies | Race.Soviet;
					else
						b.Owner = Race.None;
					continue;
				}
				
				if (s.Equals("Both", StringComparison.InvariantCultureIgnoreCase))
				{
					b.Owner = Race.Allies | Race.Soviet;
					continue;
				}
				
				string[] frags = s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (frags.Length > 1)
					b.Owner = Race.Allies | Race.Soviet;
				else
					b.Owner = (Race)Enum.Parse(typeof(Race), frags[0], true);
			}
		}

		void LoadBuildings()
		{
			foreach (string line in File.ReadAllLines("../../../buildings.txt"))
			{
				Regex pattern = new Regex(@"^(\w+),([\w ]+)$");
				Match m = pattern.Match(line);
				if (!m.Success) continue;
				units.Add(m.Groups[1].Value, new Building(m.Groups[1].Value, m.Groups[2].Value));
			}
		}

		void LoadUnits()
		{
			foreach (string line in File.ReadAllLines("../../../units.txt"))
			{
				Regex pattern = new Regex(@"^(\w+),([\w ]+)$");
				Match m = pattern.Match(line);
				if (!m.Success) continue;
				units.Add(m.Groups[1].Value, new Unit(m.Groups[1].Value, m.Groups[2].Value));
			}
		}

		public bool Build(string key)
		{
			IRAUnit b = units[key];
			if (!b.Buildable) return false;
			built.Add(key);
			CheckAll();
			return true;
		}

		public bool Unbuild(string key)
		{
			IRAUnit b = units[key];
			if (!built.Contains(key)) return false;
			built.Remove(key);
			CheckAll();
			return true;
		}

		void CheckAll()
		{
			foreach (IRAUnit unit in units.Values)
				unit.CheckPrerequisites(built, currentRace);
		}

		public IEnumerable<IRAUnit> BuildableItems
		{
			get
			{
				foreach (IRAUnit b in units.Values)
					if (b.Buildable)
						yield return b;
			}
		}
	}
}
