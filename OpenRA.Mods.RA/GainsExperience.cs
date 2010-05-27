#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class GainsExperienceInfo : ITraitInfo, ITraitPrerequisite<ValuedInfo>
	{
		public readonly float[] CostThreshold = { 2, 4, 8 };
		public readonly float[] FirepowerModifier = { 1.2f, 1.5f, 2 };
		public readonly float[] ArmorModifier = { 1.2f, 1.5f, 2 };
		public readonly float[] SpeedModifier = { 1.2f, 1.5f, 2 };
		public object Create(Actor self) { return new GainsExperience(self, this); }
	}

	public class GainsExperience : IFirepowerModifier, ISpeedModifier, IDamageModifier
	{
		readonly Actor self;
		readonly int[] Levels;
		readonly GainsExperienceInfo Info;

		public GainsExperience(Actor self, GainsExperienceInfo info)
		{
			this.self = self;
			this.Info = info;
			var cost = self.Info.Traits.Get<ValuedInfo>().Cost;
			Levels = Info.CostThreshold.Select(t => (int)(t * cost)).ToArray();
		}

		[Sync]
		int Experience = 0;
		[Sync]
		int Level = 0;

		public void GiveExperience(int amount)
		{
			Experience += amount;

			while (Level < Levels.Count() - 1 && Experience >= Levels[Level])
			{
				Level++;

//				Game.Debug("{0} became Level {1}".F(self.Info.Name, Level));

				Sound.PlayToPlayer(self.Owner, "hydrod1.aud", self.CenterLocation);
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
	}
}
