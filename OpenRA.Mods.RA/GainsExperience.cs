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

using OpenRA.GameRules;
using OpenRA.Traits;
using System.Linq;
using System.Collections.Generic;
using OpenRA.Mods.RA.Effects;
namespace OpenRA.Mods.RA
{
	public class GainsExperienceInfo : ITraitInfo
	{
		//public readonly float[] CostThreshold = {2,4,8};
		public readonly float[] CostThreshold = {1, 1.5f, 2};
		public readonly float[] FirepowerModifier = {1.2f, 1.5f, 2};
		public readonly float[] ArmorModifier = {1.2f, 1.5f, 2};
		public readonly float[] SpeedModifier = {1.2f, 1.5f, 2};
		public object Create(Actor self) { return new GainsExperience(self, this); }
	}

	public class GainsExperience: IFirepowerModifier, ISpeedModifier, IDamageModifier
	{
		readonly Actor self;
		readonly List<float> Levels;
		readonly GainsExperienceInfo Info;
		public GainsExperience(Actor self, GainsExperienceInfo info)
		{
			this.self = self;
			this.Info = info;
			System.Console.WriteLine(self.Info.Name);
			var cost = self.Info.Traits.Get<ValuedInfo>().Cost;
			Levels = Info.CostThreshold.Select(t => t*cost).ToList();
		}
		
		[Sync]
		int Experience = 0;
		[Sync]
		int Level = 0;
		
		public void GiveExperience(int amount)
		{
			// Already at max level
			if (Level == Levels.Count() - 1)
				return;
			
			Experience += amount;
			
			if (Experience > Levels[Level])
			{
				Level++;
				
				// TODO: Show an effect or play a sound or something
				System.Console.WriteLine("{0} became Level {1}",self.Info.Name, Level);
				
				self.World.AddFrameEndTask(w => 
				{
					w.Add(new CrateEffect(self, "speed"));
				});
			}
		}
		
		public float GetDamageModifier( WarheadInfo warhead )
		{
			if (Level == 0)
				return 1.0f;
			
			return 1/Info.ArmorModifier[Level - 1];
		}
		public float GetFirepowerModifier()
		{
			if (Level == 0)
				return 1.0f;
			
			return Info.FirepowerModifier[Level - 1]	;
		}
		public float GetSpeedModifier()
		{
			if (Level == 0)
				return 1.0f;
			
			return Info.SpeedModifier[Level - 1]	;
		}
	}
}
