#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor gives experience to a GainsExperience actor when they are killed.")]
	class GivesExperienceInfo : ITraitInfo
	{
		[Desc("If -1, use the value of the unit cost.")]
		public readonly int Experience = -1;

		[Desc("Grant experience for team-kills.")]
		public readonly bool FriendlyFire = false;

		public object Create(ActorInitializer init) { return new GivesExperience(init.self, this); }
	}

	class GivesExperience : INotifyKilled
	{
		readonly GivesExperienceInfo info;

		public GivesExperience(Actor self, GivesExperienceInfo info)
		{
			this.info = info;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// Prevent TK from giving exp
			if (e.Attacker == null || e.Attacker.Destroyed || (!info.FriendlyFire && e.Attacker.Owner.Stances[self.Owner] == Stance.Ally))
				return;

			var valued = self.Info.Traits.GetOrDefault<ValuedInfo>();

			// Default experience is 100 times our value
			var exp = info.Experience >= 0
				? info.Experience
				: valued != null ? valued.Cost * 100 : 0;

			var killer = e.Attacker.TraitOrDefault<GainsExperience>();
			if (killer != null)
				killer.GiveExperience(exp);
		}
	}
}
