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

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class GivesExperienceInfo : TraitInfo<GivesExperience> { public readonly int Experience = -1;	}

	class GivesExperience : INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{	
				// Prevent TK from giving exp
				if (e.Attacker == null || e.Attacker.Owner.Stances[ self.Owner ] == Stance.Ally )
					return;

				var info = self.Info.Traits.Get<GivesExperienceInfo>();
				var valued = self.Info.Traits.GetOrDefault<ValuedInfo>();

				var exp = info.Experience >= 0
					? info.Experience
					: valued != null ? valued.Cost : 0;

				var killer = e.Attacker.traits.GetOrDefault<GainsExperience>();
				if (killer != null)
					killer.GiveExperience(exp);
			}
		}
	}
}
