#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
	public class GainsExperienceInfo : ITraitInfo, Requires<ValuedInfo>
	{
		[Desc("XP requirements for each level, as multiples of our own cost.")]
		public readonly float[] CostThreshold = { 2, 4, 8, 16 };
		public readonly float[] FirepowerModifier = { 1.1f, 1.15f, 1.2f, 1.5f };
		public readonly float[] ArmorModifier = { 1.1f, 1.2f, 1.3f, 1.5f };
		public readonly decimal[] SpeedModifier = { 1.1m, 1.15m, 1.2m, 1.5m };
		public object Create(ActorInitializer init) { return new GainsExperience(init, this); }
	}

	public class GainsExperience : IFirepowerModifier, ISpeedModifier, IDamageModifier, IRenderModifier, ISync
	{
		readonly Actor self;
		readonly int[] Levels;
		readonly GainsExperienceInfo Info;
		readonly Animation RankAnim;

		public GainsExperience(ActorInitializer init, GainsExperienceInfo info)
		{
			self = init.self;
			this.Info = info;
			var cost = self.Info.Traits.Get<ValuedInfo>().Cost;
			Levels = Info.CostThreshold.Select(t => (int)(t * cost)).ToArray();
			RankAnim = new Animation("rank");
			RankAnim.PlayFetchIndex("rank", () => Level - 1);

			if (init.Contains<ExperienceInit>())
			{
				Experience = init.Get<ExperienceInit, int>();

				while (Level < Levels.Length && Experience >= Levels[Level])
					Level++;
			}
		}

		[Sync] int Experience = 0;
		[Sync] public int Level { get; private set; }

		int MaxLevel { get { return Levels.Length; } }
		public bool CanGainLevel { get { return Level < MaxLevel; } }

		public void GiveOneLevel()
		{
			if (Level < MaxLevel)
				GiveExperience(Levels[Level] - Experience);
		}

		public void GiveLevels(int numLevels)
		{
			for( var i = 0; i < numLevels; i++ )
				GiveOneLevel();
		}

		public void GiveExperience(int amount)
		{
			Experience += amount;

			while (Level < MaxLevel && Experience >= Levels[Level])
			{
				Level++;
				Sound.PlayNotification(self.Owner, "Sounds", "LevelUp", self.Owner.Country.Race);
				self.World.AddFrameEndTask(w => w.Add(new CrateEffect(self, "levelup", new int2(0,-24))));
			}
		}

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			return Level > 0 ? 1 / Info.ArmorModifier[Level - 1] : 1;
		}

		public float GetFirepowerModifier()
		{
			return Level > 0 ? Info.FirepowerModifier[Level - 1] : 1;
		}

		public decimal GetSpeedModifier()
		{
			return Level > 0 ? Info.SpeedModifier[Level - 1] : 1m;
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r)
		{
			// TODO: Make this consistent with everything else that adds animations to RenderSimple.
			if (self.Owner.IsAlliedWith(self.World.RenderPlayer) && Level > 0)
				return InnerModifyRender(self, wr, r);
			else
				return r;
		}

		IEnumerable<Renderable> InnerModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r)
		{
			foreach (var rs in r)
				yield return rs;

			RankAnim.Tick();	// HACK

			if (self.World.FogObscures(self))
				yield break;

			var bounds = self.Bounds.Value;
			yield return new Renderable(RankAnim.Image, new float2(bounds.Right - 6, bounds.Bottom - 8),
				wr.Palette("effect"), self.CenterLocation.Y);
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
