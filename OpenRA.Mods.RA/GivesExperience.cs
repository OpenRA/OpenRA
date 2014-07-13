#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor gives experience to a GainsExperience actor when they are killed.")]
	class GivesExperienceInfo : TraitInfo<GivesExperience>
	{
		[Desc("If -1, use the value of the unit cost.")]
		public readonly int Experience = -1;
	}

	class GivesExperience : INotifyKilled
	{
		public void Killed(Actor self, AttackInfo e)
		{
			// Prevent TK from giving exp
			if (e.Attacker == null || e.Attacker.Destroyed || e.Attacker.Owner.Stances[self.Owner] == Stance.Ally)
				return;

			var info = self.Info.Traits.Get<GivesExperienceInfo>();
			var valued = self.Info.Traits.GetOrDefault<ValuedInfo>();

			var exp = info.Experience >= 0
				? info.Experience
				: valued != null ? valued.Cost : 0;

			var killer = e.Attacker.TraitOrDefault<GainsExperience>();
			if (killer != null)
				killer.GiveExperience(exp);
		}
	}
}
