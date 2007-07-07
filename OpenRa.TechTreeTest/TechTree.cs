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
		Dictionary<string, Building> buildings = new Dictionary<string,Building>();
		public TechTree()
		{
			LoadBuildings();
			LoadRules();
		}

		void LoadRules()
		{
			IniFile rulesFile;
			rulesFile = new IniFile(File.OpenRead("rules.ini"));
			foreach (string key in buildings.Keys)
			{
				IniSection section = rulesFile.GetSection(key);
				Building b = buildings[key];
				string s = section.GetValue("Prerequisite", "").ToUpper();
				b.Prerequisites = s.Split(',');
				b.TechLevel = int.Parse(section.GetValue("TechLevel", "-1"));
			}
		}

		void LoadBuildings()
		{
			foreach (string line in File.ReadAllLines("buildings.txt"))
			{
				Regex pattern = new Regex(@"^(\w+),([\w ]+)$");
				Match m = pattern.Match(line);
				if (!m.Success) continue;
				buildings.Add(m.Groups[0].Value, new Building(m.Groups[1].Value));
			}
		}
	}

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
	}
}
