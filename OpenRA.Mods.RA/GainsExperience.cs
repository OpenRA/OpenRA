#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class GainsExperienceInfo : ITraitInfo, Requires<ValuedInfo>
	{
		[Desc("XP requirements for each level, as multiples of our own cost.")]
		public readonly float[] CostThreshold = { 2, 4, 8, 16 };
		public readonly float[] FirepowerModifier = { 1.1f, 1.15f, 1.2f, 1.5f };
		public readonly float[] ArmorModifier = { 1.1f, 1.2f, 1.3f, 1.5f };
		public readonly decimal[] SpeedModifier = { 1.1m, 1.15m, 1.2m, 1.5m };
		public readonly string ChevronPalette = "effect";
		public object Create(ActorInitializer init) { return new GainsExperience(init, this); }
	}

	public class GainsExperience : IFirepowerModifier, ISpeedModifier, IDamageModifier, ISync
	{
		readonly Actor self;
		readonly int[] levels;
		readonly GainsExperienceInfo info;

		public GainsExperience(ActorInitializer init, GainsExperienceInfo info)
		{
			self = init.self;
			this.info = info;
			var cost = self.Info.Traits.Get<ValuedInfo>().Cost;
			levels = info.CostThreshold.Select(t => (int)(t * cost)).ToArray();

			if (init.Contains<ExperienceInit>())
				GiveExperience(init.Get<ExperienceInit, int>());
		}

		[Sync] int experience = 0;
		[Sync] public int Level { get; private set; }

		int MaxLevel { get { return levels.Length; } }
		public bool CanGainLevel { get { return Level < MaxLevel; } }

		public void GiveOneLevel()
		{
			if (Level < MaxLevel)
				GiveExperience(levels[Level] - experience);
		}

		public void GiveLevels(int numLevels)
		{
			for (var i = 0; i < numLevels; i++)
				GiveOneLevel();
		}

		public void GiveExperience(int amount)
		{
			experience += amount;

			while (Level < MaxLevel && experience >= levels[Level])
			{
				Level++;

				Sound.PlayNotification(self.Owner, "Sounds", "LevelUp", self.Owner.Country.Race);
				self.World.AddFrameEndTask(w => w.Add(new CrateEffect(self, "levelup", info.ChevronPalette)));
				if (Level == 1)
					self.World.AddFrameEndTask(w =>
					{
						if (!self.IsDead())
							w.Add(new Rank(self, info.ChevronPalette));
					});
			}
		}

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			return Level > 0 ? 1 / info.ArmorModifier[Level - 1] : 1;
		}

		public float GetFirepowerModifier()
		{
			return Level > 0 ? info.FirepowerModifier[Level - 1] : 1;
		}

		public decimal GetSpeedModifier()
		{
			return Level > 0 ? info.SpeedModifier[Level - 1] : 1m;
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
