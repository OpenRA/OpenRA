using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;

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
			Tuple<string[], string[]> pre = ParsePrerequisites(unitInfo.Prerequisite, tag);
			alliedPrerequisites = pre.a;
			sovietPrerequisites = pre.b;
		}

		static Race ParseOwner(string owners, bool doubleOwned)
		{
			if (doubleOwned)
				return Race.Allies | Race.Soviet;

			Race race = Race.None;
			string[] frags = owners.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string s in frags)
				race |= (Race)Enum.Parse(typeof(Race), s, true);

			return race;
		}

		static Tuple<string[],string[]> ParsePrerequisites(string[] prerequisites, string tag)
		{
			List<string> allied = prerequisites.Select( x => x.ToLowerInvariant() ).ToList();
			List<string> soviet = new List<string>(allied);

			if (allied.Remove("stek"))
				allied.Add("atek");

			if (soviet.Remove("atek"))
				soviet.Add("stek");

			if (soviet.Remove("tent"))
				soviet.Add("barr");

			if (allied.Remove("barr"))
				allied.Add("tent");

			// TODO: rewrite this based on "InfantryTypes" in units.ini
			if ((tag.Length == 2 && tag[0] == 'e') || tag == "medi" || tag == "thf" || tag == "spy")
			{
				if (!allied.Contains("tent"))
					allied.Add("tent");
				if (!soviet.Contains("barr"))
					soviet.Add("barr");
			}

			if (tag == "lst")
			{
				if (!soviet.Contains("spen"))
					soviet.Add("spen");

				if (!allied.Contains("syrd"))
					allied.Add("syrd");
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

			List<string> p = new List<string>(racePrerequisites);
			foreach (string b in buildings)
				p.Remove(b);

			return p.Count == 0;
		}

		bool ShouldMakeUnbuildable(IEnumerable<string> buildings, string[] racePrerequisites)
		{
			if (racePrerequisites.Length == 0)
				return false;

			List<string> p = new List<string>(racePrerequisites);
			foreach (string b in buildings)
				p.Remove(b);

			return p.Count == racePrerequisites.Length;
		}

		void CheckForBoth(IEnumerable<string> buildings)
		{
			if (canBuild && (ShouldMakeUnbuildable(buildings, alliedPrerequisites) && ShouldMakeUnbuildable(buildings, sovietPrerequisites)))
				canBuild = false;

			else if (!canBuild && (ShouldMakeBuildable(buildings, alliedPrerequisites) || ShouldMakeBuildable(buildings, sovietPrerequisites)))
				canBuild = true;
		}

		public void CheckPrerequisites(IEnumerable<string> buildings, Race currentRace)
		{
			if (currentRace == Race.None || currentRace == (Race.Allies | Race.Soviet))
				CheckForBoth(buildings);
			else
			{
				string[] racePrerequisites = (currentRace == Race.Allies) ? alliedPrerequisites : sovietPrerequisites;

				if ((canBuild && ShouldMakeUnbuildable(buildings, racePrerequisites)) || !((owner & currentRace) == currentRace))
					canBuild = false;
				else if (!canBuild && ShouldMakeBuildable(buildings, racePrerequisites))
					canBuild = true;
			}
		}

		bool canBuild;
		public bool CanBuild { get { return canBuild; } }
		public string Tooltip { get { return string.Format("{0} ({1})\n{2}", friendlyName, tag, owner); } }
	}
}
