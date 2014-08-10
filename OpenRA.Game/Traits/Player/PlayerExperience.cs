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
<<<<<<< HEAD
		public readonly Dictionary<int, string[]> ExperienceLevels = null;
=======
		public readonly Dictionary<int, int> ExperienceLevels = null;
>>>>>>> c0199c14612df02157eb3cd1de4bf0a3602bcc4e

		public object Create(ActorInitializer init) { return new PlayerExperience(init.self, this); }
		static object LoadExperience(MiniYaml y)
		{
			MiniYaml experienceLevel;

			if (!y.ToDictionary().TryGetValue("Experience", out experienceLevel))
			{
<<<<<<< HEAD
				return new Dictionary<int, string[]>()
				{
					{ 0, new[] { "sciencerank1", "sciencepoint" } },
					{ 800, new[] { "sciencerank2", "sciencepoint" } },
					{ 1500, new[] { "sciencerank3", "sciencepoint" } },
					{ 2500, new[] { "sciencerank4", "sciencepoint" } },
					{ 3500, new[] { "sciencerank5", "sciencepoint" } },
					{ 5000, new[] { "sciencerank6", "sciencepoint" } },
=======
				return new Dictionary<int, int>()
				{
					{ 0, 1 },
					{ 800, 1 },
					{ 1500, 1 },
					{ 2500, 1 },
					{ 3500, 1 },
					{ 5000, 1 },
>>>>>>> c0199c14612df02157eb3cd1de4bf0a3602bcc4e
				};
			}

			return experienceLevel.Nodes.ToDictionary(
				kv => FieldLoader.GetValue<int>("(key)", kv.Key),
<<<<<<< HEAD
				kv => FieldLoader.GetValue<string[]>("(value)", kv.Value.Value));
=======
				kv => FieldLoader.GetValue<int>("(value)", kv.Value.Value));
>>>>>>> c0199c14612df02157eb3cd1de4bf0a3602bcc4e
		}
	}

	public class PlayerExperience : ISync
	{
<<<<<<< HEAD
		readonly List<Pair<int, string[]>> nextLevel = new List<Pair<int, string[]>>();
=======
		readonly List<Pair<int, int>> nextLevel = new List<Pair<int, int>>();
>>>>>>> c0199c14612df02157eb3cd1de4bf0a3602bcc4e

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


		public void GiveExperience(int num)
		{
			Experience += num;
			while (Rank < MaxRank && Experience >= nextLevel[Rank].First)
			{
<<<<<<< HEAD
				//Temporary fix until it is implemented properly as upgrades.
				LevelUp(1);//nextLevel[Rank].Second);
=======
				LevelUp(nextLevel[Rank].Second);
>>>>>>> c0199c14612df02157eb3cd1de4bf0a3602bcc4e
			}
		}

		public void LevelUp(int num) 
		{
			Rank++;
			GiveSciencePoints(num);
		}

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
