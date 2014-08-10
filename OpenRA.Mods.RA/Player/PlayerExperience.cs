#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Graphics;

//namespace OpenRA.Traits
namespace OpenRA.Mods.RA
{
	public class PlayerExperienceInfo : ITraitInfo
	{
		[FieldLoader.LoadUsing("LoadExperience")]
		[Desc("At which level should # sciencepoints be bestowed")]
		public readonly Dictionary<int, string[]> ExperienceLevels = null;

		public object Create(ActorInitializer init) { return new PlayerExperience(init.self, this); }
		static object LoadExperience(MiniYaml y)
		{
			MiniYaml experienceLevel;

			if (!y.ToDictionary().TryGetValue("Experience", out experienceLevel))
			{
				return new Dictionary<int, string[]>()
				{
					{ 0, new[] { "sciencerank1", "sciencepoint" } },
					{ 800, new[] { "sciencerank2", "sciencepoint" } },
					{ 1500, new[] { "sciencerank3", "sciencepoint" } },
					{ 2500, new[] { "sciencerank4", "sciencepoint" } },
					{ 3500, new[] { "sciencerank5", "sciencepoint" } },
					{ 5000, new[] { "sciencerank6", "sciencepoint" } },
				};
			}

			return experienceLevel.Nodes.ToDictionary(
				kv => FieldLoader.GetValue<int>("(key)", kv.Key),
				kv => FieldLoader.GetValue<string[]>("(value)", kv.Value.Value));
		}
	}

	public class PlayerExperience : ISync, ITechTreeElement
	{
		readonly List<Pair<int, string[]>> nextLevel = new List<Pair<int, string[]>>();
		public readonly	TechTree TechTree;
		public readonly Actor self;

		public PlayerExperience(Actor self, PlayerExperienceInfo info)
		{
			Experience = 0;
			Rank = 0;
			SciencePoints = 0;

			MaxRank = info.ExperienceLevels.Count;

			TechTree = self.Trait<TechTree>();

			foreach (var kv in info.ExperienceLevels)
				nextLevel.Add(Pair.New(kv.Key, kv.Value));
			/* Init with 0, to advance the first rank, if so defined in player.yaml */
			GiveExperience(0);
		}

		[Sync] public int Experience;
		[Sync] public int Rank;
		[Sync] public int SciencePoints;

		public readonly int MaxRank;

		public void GiveExperience(int num)
		{
			Experience += num;
			while (Rank < MaxRank && Experience >= nextLevel[Rank].First)
			{
				LevelUp(nextLevel[Rank].Second);
			}
		}

		public void LevelUp(string[] awardedSciences) 
		{
			Rank++;
			foreach (string science in awardedSciences)
			{
				//Console.WriteLine(science);
				if(science == "sciencepoint")
				{
					GiveSciencePoints(1); //Move to ScienceManager object
				} else{
					//Somehow add this to the techtree.
					//TechTree.Add(science, t.Info.Prerequisites, 0, this);
					//ITechTreeElement tte = new ITechTreeElement;
					TechTree.Add(science, new[] { science }, 0, this);
					TechTree.Update();
				}
			}
		}

		/* Oh Pasta this feels dirty */
		public void PrerequisitesAvailable(string key) { }
		public void PrerequisitesUnavailable(string key) { }
		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }

		/* Move these to seperate trait*/
		public void GiveSciencePoints(int num)
		{
			SciencePoints += num;
		}

		public bool UseSciencePoints(int num)
		{
			if (num > SciencePoints) return false;
			SciencePoints -= num;
			return true;
		}

	}
}
