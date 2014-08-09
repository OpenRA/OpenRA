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

namespace OpenRA.Traits
{
	public class PlayerExperienceInfo : ITraitInfo
	{
		[FieldLoader.LoadUsing("LoadExperience")]
		[Desc("At which level should # sciencepoints be bestowed")]
		public readonly Dictionary<int, int> ExperienceLevels = null;

		public object Create(ActorInitializer init) { return new PlayerExperience(init.self, this); }
		static object LoadExperience(MiniYaml y)
		{
			MiniYaml experienceLevel;

			if (!y.ToDictionary().TryGetValue("Experience", out experienceLevel))
			{
				return new Dictionary<int, int>()
				{
					{ 0, 1 },
					{ 800, 1 },
					{ 1500, 1 },
					{ 2500, 1 },
					{ 3500, 1 },
					{ 5000, 1 },
				};
			}

			return experienceLevel.Nodes.ToDictionary(
				kv => FieldLoader.GetValue<int>("(key)", kv.Key),
				kv => FieldLoader.GetValue<int>("(value)", kv.Value.Value));
		}
		public object Create(ActorInitializer init) { return new PlayerExperience(init.self, this); }
	}

	public class PlayerExperience : ISync
	{
		readonly List<Pair<int, int>> nextLevel = new List<Pair<int, int>>();

		public PlayerExperience(Actor self, PlayerExperienceInfo info)
		{
			Experience = 0;
			Rank = 0;
			SciencePoints = 0;

			MaxRank = info.ExperienceLevels.Count;

			foreach (var kv in info.ExperienceLevels)
				nextLevel.Add(Pair.New(kv.Key, kv.Value));
			/* Init with 0, to advance the first rank, if so defined in player.yaml */
			GiveExperience(0);
		}

		[Sync] public int Experience;
		[Sync] public int Rank;
		[Sync] public int SciencePoints;

		public readonly int MaxRank;
		//readonly Player Owner;

		public PlayerExperience(Actor self, PlayerExperienceInfo info)
		{
			//Owner = self.Owner;

			Experience = 0;
			Rank = 0;
			SciencePoints = 0;
		}

		[Sync] public int Experience;
		[Sync] public int Rank;

		[Sync] public int SciencePoints;


		public void GiveExperience(int num)
		{
			Experience += num;
			while (Rank < MaxRank && Experience >= nextLevel[Rank].First)
			{
				LevelUp(nextLevel[Rank].Second);
			}
		}

		public void LevelUp(int num) 
		{
			Rank++;
			GiveSciencePoints(num);
		}

		public void LevelUp() 
		{
			Rank++;
		}

		public void GiveSciencePoint(int num)
		{
			SciencePoints += num;
		}

		public bool UseSciencePoint(int num)
		{
			if(num > SciencePoints) return false;
			SciencePoints -= num;
			return true;
		}

	}
}
