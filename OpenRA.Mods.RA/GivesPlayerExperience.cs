#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("You get player experience for playing this actor.")]
	class GivesPlayerExperienceInfo : TraitInfo<GivesPlayerExperience>
	{
		public readonly int Percentage = 100;
		[Desc("Higher ranked units give higher bounties.")]
		public readonly int LevelMod = 125;
		[Desc("Destroying creeps and enemies is rewarded.")]
		public readonly Stance[] Stances = { Stance.Neutral, Stance.Enemy };
	}

	class GivesPlayerExperience : INotifyKilled
	{
		static int GetMultiplier(Actor self)
		{
			// returns 100's as 1, so as to keep accuracy for longer.
			var info = self.Info.Traits.Get<GivesPlayerExperienceInfo>();
			var gainsExp = self.TraitOrDefault<GainsExperience>();
			if (gainsExp == null)
				return 100;

			var slevel = gainsExp.Level;
			return (slevel > 0) ? slevel * info.LevelMod : 100;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			var info = self.Info.Traits.Get<GivesPlayerExperienceInfo>();

			if (e.Attacker == null || e.Attacker.Destroyed)	return;

			if (!info.Stances.Contains(e.Attacker.Owner.Stances[self.Owner])) return;

			var cost = self.GetSellValue();
			// 2 hundreds because of GetMultiplier and info.Percentage.
			var bounty = cost * GetMultiplier(self) * info.Percentage / 10000;

			e.Attacker.Owner.PlayerActor.Trait<PlayerExperience>().GiveExperience(bounty);
		}
	}
}
