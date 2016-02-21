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

using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("You get money for playing this actor.")]
	class GivesBountyInfo : TraitInfo<GivesBounty>
	{
		[Desc("Calculated by Cost or CustomSellValue so they have to be set to avoid crashes.")]
		public readonly int Percentage = 10;
		[Desc("Higher ranked units give higher bounties.")]
		public readonly int LevelMod = 125;
		[Desc("Destroying creeps and enemies is rewarded.")]
		public readonly Stance[] Stances = { Stance.Neutral, Stance.Enemy };
	}

	class GivesBounty : INotifyKilled
	{
		static int GetMultiplier(Actor self)
		{
			// returns 100's as 1, so as to keep accuracy for longer.
			var info = self.Info.TraitInfo<GivesBountyInfo>();
			var gainsExp = self.TraitOrDefault<GainsExperience>();
			if (gainsExp == null)
				return 100;

			var slevel = gainsExp.Level;
			return (slevel > 0) ? slevel * info.LevelMod : 100;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			var info = self.Info.TraitInfo<GivesBountyInfo>();

			if (e.Attacker == null || e.Attacker.Disposed) return;

			if (!info.Stances.Contains(e.Attacker.Owner.Stances[self.Owner])) return;

			var cost = self.GetSellValue();

			// 2 hundreds because of GetMultiplier and info.Percentage.
			var bounty = cost * GetMultiplier(self) * info.Percentage / 10000;

			if (bounty > 0 && e.Attacker.Owner.IsAlliedWith(self.World.RenderPlayer))
				e.Attacker.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, e.Attacker.Owner.Color.RGB, FloatingText.FormatCashTick(bounty), 30)));

			e.Attacker.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(bounty);
		}
	}
}
