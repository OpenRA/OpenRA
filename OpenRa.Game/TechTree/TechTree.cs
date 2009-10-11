using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OpenRa.FileFormats;
using System.Linq;
using OpenRa.Game;

namespace OpenRa.TechTree
{
	public class TechTree
	{
		Dictionary<string, Item> objects = new Dictionary<string, Item>();
		public ICollection<string> built = new List<string>();

		Race currentRace = Race.None;

		public Race CurrentRace
		{
			get { return currentRace; }
			set 
			{ 
				currentRace = value;
				CheckAll();
			}
		}

		public TechTree()
		{
			LoadRules();
			CheckAll();
		}

		IEnumerable<Tuple<string, string, bool>> Lines(string filename, bool param)
		{
			Regex pattern = new Regex(@"^(\w+),([\w ]+),(\w+)$");
			foreach (string s in File.ReadAllLines("../../../../" + filename))
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
			IEnumerable<Tuple<string, string, bool>> definitions = 
				Lines("buildings.txt", true)
				.Concat( Lines( "vehicles.txt", false ) )
				.Concat( Lines( "infantry.txt", false ) );

            foreach (Tuple<string, string, bool> p in definitions)
				objects.Add(p.a, new Item(p.a, p.b, Rules.UnitInfo.Get(p.a), p.c));
		}

		public bool Build(string key, bool force)
		{
			if( string.IsNullOrEmpty( key ) ) return false;
			key = key.ToUpperInvariant();
			Item b = objects[ key ];
			if (!force && !b.CanBuild) return false;
			built.Add(key);
			CheckAll();
			return true;
		}

		public bool Build(string key)
		{
			return Build(key, false);
		}

		public bool Unbuild(string key)
		{
			key = key.ToUpperInvariant();
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

			BuildableItemsChanged();
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

		public event Action BuildableItemsChanged = () => { };
	}
}
