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

using System.Collections.Generic;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("When killed, this actor causes the attacking player to receive money.")]
	class GivesBountyInfo : ITraitInfo
	{
		[Desc("Percentage of the killed actor's Cost or CustomSellValue to be given.")]
		public readonly int Percentage = 10;

		[Desc("Scale bounty based on the veterancy of the killed unit. The value is given in percent.")]
		public readonly int LevelMod = 125;

		[Desc("Stance the attacking player needs to receive the bounty.")]
		public readonly Stance ValidStances = Stance.Neutral | Stance.Enemy;

		[Desc("Whether to show a floating text announcing the won bounty.")]
		public readonly bool ShowBounty = true;

		[Desc("DeathTypes for which a bounty should be granted.",
			"Use an empty list (the default) to allow all DeathTypes.")]
		public readonly HashSet<string> DeathTypes = new HashSet<string>();

		public object Create(ActorInitializer init) { return new GivesBounty(init.Self, this); }
	}

	class GivesBounty : INotifyKilled, INotifyCreated
	{
		readonly GivesBountyInfo info;
		GainsExperience gainsExp;
		Cargo cargo;

		public GivesBounty(Actor self, GivesBountyInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			gainsExp = self.TraitOrDefault<GainsExperience>();
			cargo = self.TraitOrDefault<Cargo>();
		}

		// Returns 100's as 1, so as to keep accuracy for longer.
		int GetMultiplier()
		{
			if (gainsExp == null)
				return 100;

			var slevel = gainsExp.Level;
			return (slevel > 0) ? slevel * info.LevelMod : 100;
		}

		int GetBountyValue(Actor self)
		{
			// Divide by 10000 because of GetMultiplier and info.Percentage.
			return self.GetSellValue() * GetMultiplier() * info.Percentage / 10000;
		}

		int GetDisplayedBountyValue(Actor self)
		{
			var bounty = GetBountyValue(self);
			if (cargo == null)
				return bounty;

			foreach (var a in cargo.Passengers)
			{
				var givesBounty = a.TraitOrDefault<GivesBounty>();
				if (givesBounty != null)
					bounty += givesBounty.GetDisplayedBountyValue(a);
			}

			return bounty;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (!info.ValidStances.HasStance(e.Attacker.Owner.Stances[self.Owner]))
				return;

			if (info.DeathTypes.Count > 0 && !e.Damage.DamageTypes.Overlaps(info.DeathTypes))
				return;

			var displayedBounty = GetDisplayedBountyValue(self);
			if (info.ShowBounty && self.IsInWorld && displayedBounty > 0 && e.Attacker.Owner.IsAlliedWith(self.World.RenderPlayer))
				e.Attacker.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, e.Attacker.Owner.Color.RGB, FloatingText.FormatCashTick(displayedBounty), 30)));

			e.Attacker.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(GetBountyValue(self));
		}
	}
}
