#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class GainsExperienceInfo : ITraitInfo, ITraitPrerequisite<ValuedInfo>
	{
		public readonly float[] CostThreshold = { 2, 4, 8, 16 };
		public readonly float[] FirepowerModifier = { 1.1f, 1.15f, 1.2f, 1.5f };
		public readonly float[] ArmorModifier = { 1.1f, 1.2f, 1.3f, 1.5f };
		public readonly float[] SpeedModifier = { 1.1f, 1.15f, 1.2f, 1.5f };
		public object Create(ActorInitializer init) { return new GainsExperience(init.self, this); }
	}

	public class GainsExperience : IFirepowerModifier, ISpeedModifier, IDamageModifier, IRenderModifier
	{
		readonly Actor self;
		readonly int[] Levels;
		readonly GainsExperienceInfo Info;
		readonly Animation RankAnim;

		public GainsExperience(Actor self, GainsExperienceInfo info)
		{
			this.self = self;
			this.Info = info;
			var cost = self.Info.Traits.Get<ValuedInfo>().Cost;
			Levels = Info.CostThreshold.Select(t => (int)(t * cost)).ToArray();
			RankAnim = new Animation("rank");
			RankAnim.PlayFetchIndex("rank", () => Level - 1);
		}

		[Sync]
		int Experience = 0;
		[Sync]
		int Level = 0;

		public void GiveOneLevel()
		{
			if (Level < Levels.Count())
				GiveExperience(Levels[Level] - Experience);
		}

		public void GiveExperience(int amount)
		{
			Experience += amount;

			while (Level < Levels.Count() && Experience >= Levels[Level])
			{
				Level++;

//				Game.Debug("{0} became Level {1}".F(self.Info.Name, Level));
				var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
				Sound.PlayToPlayer(self.Owner, eva.LevelUp, self.CenterLocation);
				self.World.AddFrameEndTask(w => w.Add(new CrateEffect(self, "levelup", new int2(0,-24))));
			}
		}

		public float GetDamageModifier(WarheadInfo warhead)
		{
			return Level > 0 ? 1 / Info.ArmorModifier[Level - 1] : 1;
		}

		public float GetFirepowerModifier()
		{
			return Level > 0 ? Info.FirepowerModifier[Level - 1] : 1;
		}

		public float GetSpeedModifier()
		{
			return Level > 0 ? Info.SpeedModifier[Level - 1] : 1;
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> rs)
		{
			foreach (var r in rs)
				yield return r;

			if (self.Owner == Game.world.LocalPlayer && Level > 0)
			{
				RankAnim.Tick();	// hack
				var bounds = self.GetBounds(true);
				yield return new Renderable(RankAnim.Image, 
					new float2(bounds.Right - 6, bounds.Bottom - 8), "effect");
			}
		}
	}
}
