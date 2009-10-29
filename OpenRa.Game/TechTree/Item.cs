using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;
using IjwFramework.Types;
using OpenRa.Game;

namespace OpenRa.TechTree
{
	public class Item
	{
		readonly bool isStructure;

		public bool IsStructure { get { return isStructure; } }

		public Item(string tag, UnitInfo unitInfo, bool isStructure)
		{
			this.tag = tag;
			this.friendlyName = unitInfo.Description;
			this.isStructure = isStructure;

			owner = ParseOwner(unitInfo.Owner, unitInfo.DoubleOwned);
			techLevel = unitInfo.TechLevel;
			var pre = ParsePrerequisites(unitInfo.Prerequisite, tag);
			alliedPrerequisites = pre.a;
			sovietPrerequisites = pre.b;
		}

		static Race ParseOwner(string[] owners, bool doubleOwned)
		{
			if (doubleOwned)
				return Race.Allies | Race.Soviet;

			Race race = Race.None;

			foreach (string s in owners)
				race |= Enum<Race>.Parse(s);

			return race;
		}

		static Tuple<string[],string[]> ParsePrerequisites(string[] prerequisites, string tag)
		{
			var allied = prerequisites.Select( x => x.ToLowerInvariant() ).ToList();
			var soviet = new List<string>(allied);

			if (allied.Remove("stek")) allied.Add("atek");
			if (soviet.Remove("atek")) soviet.Add("stek");
			if (soviet.Remove("tent")) soviet.Add("barr");
			if (allied.Remove("barr")) allied.Add("tent");

			if( Rules.Categories[ "InfantryTypes" ].Contains( tag ) )
			{
				if( !allied.Contains( "tent" ) ) allied.Add( "tent" );
				if( !soviet.Contains( "barr" ) ) soviet.Add( "barr" );
			}

			if (tag == "lst")
			{
				if (!soviet.Contains("spen")) soviet.Add("spen");
				if (!allied.Contains("syrd")) allied.Add("syrd");
			}

			return new Tuple<string[], string[]>(
				allied.ToArray(), soviet.ToArray());
		}

		public readonly string tag, friendlyName;
		readonly int techLevel;
		readonly Race owner;
		readonly string[] alliedPrerequisites, sovietPrerequisites;

		bool ShouldMakeBuildable(IEnumerable<string> buildings, string[] racePrerequisites)
		{
			if (techLevel < 0)
				return false;

			if (racePrerequisites.Length == 0)
				return true;

			return racePrerequisites.Except(buildings).Count() == 0;
		}

		bool ShouldMakeUnbuildable(IEnumerable<string> buildings, string[] racePrerequisites)
		{
			if (racePrerequisites.Length == 0)
				return false;

			return racePrerequisites.Except(buildings).Count() == racePrerequisites.Length;
		}

		void CheckForBoth(IEnumerable<string> buildings)
		{
			if (CanBuild && (ShouldMakeUnbuildable(buildings, alliedPrerequisites) 
				&& ShouldMakeUnbuildable(buildings, sovietPrerequisites)))
				CanBuild = false;

			else if (!CanBuild && (ShouldMakeBuildable(buildings, alliedPrerequisites) 
				|| ShouldMakeBuildable(buildings, sovietPrerequisites)))
				CanBuild = true;
		}

		public void CheckPrerequisites(IEnumerable<string> buildings, Race currentRace)
		{
			if (currentRace == Race.None || currentRace == (Race.Allies | Race.Soviet))
				CheckForBoth(buildings);
			else
			{
				string[] racePrerequisites = (currentRace == Race.Allies) ? alliedPrerequisites : sovietPrerequisites;

				if ((CanBuild && ShouldMakeUnbuildable(buildings, racePrerequisites)) || !((owner & currentRace) == currentRace))
					CanBuild = false;
				else if (!CanBuild && ShouldMakeBuildable(buildings, racePrerequisites))
					CanBuild = true;
			}
		}

		public bool CanBuild { get; private set; }
		public string Tooltip { get { return string.Format("{0} ({1})\n{2}", friendlyName, tag, owner); } }
	}
}
