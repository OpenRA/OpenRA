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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("When killed, this actor causes nearby actors with the ProximityBounty trait to receive money.")]
	class GivesProximityBountyInfo : ITraitInfo
	{
		[Desc("Percentage of the killed actor's Cost or CustomSellValue to be given.")]
		public readonly int Percentage = 10;

		[Desc("Scale bounty based on the veterancy of the killed unit. The value is given in percent.")]
		public readonly int LevelMod = 125;

		[Desc("Stance the attacking player needs to grant bounty to actors.")]
		public readonly Stance ValidStances = Stance.Neutral | Stance.Enemy;

		[Desc("DeathTypes for which a bounty should be granted.",
		      "Use an empty list (the default) to allow all DeathTypes.")]
		public readonly HashSet<string> DeathTypes = new HashSet<string>();

		[Desc("Bounty types for the ProximityBounty traits which a bounty should be granted.",
		      "Use an empty list (the default) to allow all of them.")]
		public readonly HashSet<string> BountyTypes = new HashSet<string>();

		public object Create(ActorInitializer init) { return new GivesProximityBounty(init.Self, this); }
	}

	class GivesProximityBounty : INotifyKilled
	{
		readonly GivesProximityBountyInfo info;
		public HashSet<ProximityBounty> Collectors;

		public GivesProximityBounty(Actor self, GivesProximityBountyInfo info)
		{
			this.info = info;
			Collectors = new HashSet<ProximityBounty>();
		}

		int GetMultiplier(Actor self)
		{
			// returns 100's as 1, so as to keep accuracy for longer.
			var gainsExp = self.TraitOrDefault<GainsExperience>();
			if (gainsExp == null)
				return 100;

			var slevel = gainsExp.Level;
			return (slevel > 0) ? slevel * info.LevelMod : 100;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!Collectors.Any())
				return;

			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (!info.ValidStances.HasStance(e.Attacker.Owner.Stances[self.Owner]))
				return;

			if (info.DeathTypes.Count > 0 && !e.Damage.DamageTypes.Overlaps(info.DeathTypes))
				return;

			var cost = self.GetSellValue();

			// 2 hundreds because of GetMultiplier and info.Percentage.
			var bounty = cost * GetMultiplier(self) * info.Percentage / 10000;

			foreach (var c in Collectors)
			{
				if (info.BountyTypes.Count > 0 && !info.BountyTypes.Contains(c.Info.BountyType))
					return;

				if (!c.Info.ValidStances.HasStance(e.Attacker.Owner.Stances[self.Owner]))
					return;

				c.AddBounty(bounty);
			}
		}
	}
}
