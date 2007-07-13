using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.IO;
using System.Text.RegularExpressions;

namespace OpenRa.TechTree
{
	public class TechTree
	{
		Dictionary<string, Item> objects = new Dictionary<string, Item>();
		public ICollection<string> built = new List<string>();

		readonly Race currentRace;

		public TechTree(Race race)
		{
			this.currentRace = race;
			LoadRules();

			built.Add("FACT");
			CheckAll();
		}

		static IEnumerable<T> Concat<T>(IEnumerable<T> one, IEnumerable<T> two)
		{
			foreach (T t in one)
				yield return t;
			foreach (T t in two)
				yield return t;
		}

		IEnumerable<Tuple<string, string, bool>> Lines(string filename, bool param)
		{
			Regex pattern = new Regex(@"^(\w+),([\w ]+)$");
			foreach (string s in File.ReadAllLines(filename))
			{
				Match m = pattern.Match(s);
				if (m == null || !m.Success)
					continue;

				yield return new Tuple<string, string, bool>(
					m.Groups[1].Value, m.Groups[2].Value, param);
			}
		}

		void LoadRules()
		{
			IniFile rulesFile = new IniFile(File.OpenRead("../../../rules.ini"));
			IEnumerable<Tuple<string, string, bool>> definitions = Concat(
				Lines("../../../buildings.txt", true),
				Lines("../../../units.txt", false));

			foreach (Tuple<string, string, bool> p in definitions)
				objects.Add(p.a, new Item(p.a, p.b, rulesFile.GetSection(p.a), p.c));
		}

		public bool Build(string key)
		{
			Item b = objects[key];
			if (!b.CanBuild) return false;
			built.Add(key);
			CheckAll();
			return true;
		}

		public bool Unbuild(string key)
		{
			Item b = objects[key];
			if (!built.Contains(key)) return false;
			built.Remove(key);
			CheckAll();
			return true;
		}

		void CheckAll()
		{
			foreach (Item unit in objects.Values)
				unit.CheckPrerequisites(built, currentRace);
		}

		public IEnumerable<Item> BuildableItems
		{
			get
			{
				foreach (Item b in objects.Values)
					if (b.CanBuild)
						yield return b;
			}
		}
	}
}
