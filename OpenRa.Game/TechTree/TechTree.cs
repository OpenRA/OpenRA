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

		void LoadRules()
		{
			var allBuildings = Rules.AllRules.GetSection( "BuildingTypes" ).Select( x => x.Key.ToLowerInvariant() ).ToList();

			foreach( var unit in Rules.UnitInfo )
				objects.Add( unit.Key, new Item( unit.Key, unit.Value, allBuildings.Contains( unit.Key ) ) );
		}

		public bool Build(string key, bool force)
		{
			if( string.IsNullOrEmpty( key ) ) return false;
			key = key.ToLowerInvariant();
			Item b = objects[ key ];
			if (!force && !b.CanBuild) return false;
			built.Add(key);
			CheckAll();
			return true;
		}

		public bool Build(string key) { return Build(key, false); }

		public bool Unbuild(string key)
		{
			key = key.ToLowerInvariant();
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

		public IEnumerable<Item> BuildableItems { get { return objects.Values.Where(b => b.CanBuild); } }
		public event Action BuildableItemsChanged = () => { };
	}
}
