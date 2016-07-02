#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor gives experience to a GainsExperience actor when they are killed.")]
	class GivesExperienceInfo : ITraitInfo
	{
		[Desc("If -1, use the value of the unit cost.")]
		public readonly int Experience = -1;

		[Desc("Stance the attacking player needs to receive the experience.")]
		public readonly Stance ValidStances = Stance.Neutral | Stance.Enemy;

		public object Create(ActorInitializer init) { return new GivesExperience(init.Self, this); }
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
			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (!info.ValidStances.HasStance(e.Attacker.Owner.Stances[self.Owner]))
				return;

			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();

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
