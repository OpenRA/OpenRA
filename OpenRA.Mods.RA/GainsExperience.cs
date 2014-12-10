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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.RA.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor's experience increases when it has killed a GivesExperience actor.")]
	public class GainsExperienceInfo : ITraitInfo, Requires<ValuedInfo>
	{
		[FieldLoader.LoadUsing("LoadConditions")]
		[Desc("Condition types to grant at each level",
			"Key is the XP requirements for each level as a percentage of our own value.",
			"Value is a list of the condition types to grant")]
		public readonly Dictionary<int, string[]> Conditions = null;

		[Desc("Palette for the chevron glyph rendered in the selection box.")]
		public readonly string ChevronPalette = "effect";

		[Desc("Palette for the level up sprite.")]
		public readonly string LevelUpPalette = "effect";

		public object Create(ActorInitializer init) { return new GainsExperience(init, this); }

		static object LoadConditions(MiniYaml y)
		{
			MiniYaml conditions;

			if (!y.ToDictionary().TryGetValue("Conditions", out conditions))
			{
				return new Dictionary<int, string[]>()
				{
					{ 200, new[] { "experience", "firepower", "damage", "speed", "reload", "inaccuracy" } },
					{ 400, new[] { "experience", "firepower", "damage", "speed", "reload", "inaccuracy" } },
					{ 800, new[] { "experience", "firepower", "damage", "speed", "reload", "inaccuracy" } },
					{ 1600, new[] { "experience", "firepower", "damage", "speed", "reload", "inaccuracy", "elite", "selfheal" } }
				};
			}

			return conditions.Nodes.ToDictionary(
				kv => FieldLoader.GetValue<int>("(key)", kv.Key),
				kv => FieldLoader.GetValue<string[]>("(value)", kv.Value.Value));
		}
	}

	public class GainsExperience : ISync
	{
		readonly Actor self;
		readonly GainsExperienceInfo info;

		readonly List<Pair<int, string[]>> nextLevel = new List<Pair<int, string[]>>();

		// Stored as a percentage of our value
		[Sync] int experience = 0;

		[Sync] public int Level { get; private set; }
		public readonly int MaxLevel;

		public GainsExperience(ActorInitializer init, GainsExperienceInfo info)
		{
			self = init.self;
			this.info = info;

			MaxLevel = info.Conditions.Count;

			var cost = self.Info.Traits.Get<ValuedInfo>().Cost;
			foreach (var kv in info.Conditions)
				nextLevel.Add(Pair.New(kv.Key * cost, kv.Value));

			if (init.Contains<ExperienceInit>())
				GiveExperience(init.Get<ExperienceInit, int>());
		}

		public bool CanGainLevel { get { return Level < MaxLevel; } }

		public void GiveLevels(int numLevels)
		{
			var newLevel = Math.Min(Level + numLevels, MaxLevel);
			GiveExperience(nextLevel[newLevel - 1].First - experience);
		}

		public void GiveExperience(int amount)
		{
			experience += amount;

			while (Level < MaxLevel && experience >= nextLevel[Level].First)
			{
				var conditions = nextLevel[Level].Second;

				Level++;

				var um = self.TraitOrDefault<ConditionManager>();
				if (um != null)
					foreach (var u in conditions)
						um.GrantCondition(self, u, this);

				Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", "LevelUp", self.Owner.Country.Race);
				self.World.AddFrameEndTask(w => w.Add(new CrateEffect(self, "levelup", info.LevelUpPalette)));

				if (Level == 1)
				{
					self.World.AddFrameEndTask(w =>
					{
						if (!self.IsDead)
							w.Add(new Rank(self, info.ChevronPalette));
					});
				}
			}
		}
	}

	class ExperienceInit : IActorInit<int>
	{
		[FieldFromYamlKey] public readonly int value = 0;
		public ExperienceInit() { }
		public ExperienceInit(int init) { value = init; }
		public int Value(World world) { return value; }
	}
}
