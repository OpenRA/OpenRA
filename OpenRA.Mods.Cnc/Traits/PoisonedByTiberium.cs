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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class PoisonedByTiberiumInfo : UpgradableTraitInfo, IRulesetLoaded
	{
		[WeaponReference] public readonly string Weapon = "Tiberium";
		public readonly HashSet<string> Resources = new HashSet<string> { "Tiberium", "BlueTiberium" };

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new PoisonedByTiberium(init, this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai) { WeaponInfo = rules.Weapons[Weapon.ToLowerInvariant()]; }
	}

	class PoisonedByTiberium : UpgradableTrait<PoisonedByTiberiumInfo>, ITick, ISync
	{
		readonly ResourceLayer rl;
		[Sync] int poisonTicks;

		public PoisonedByTiberium(ActorInitializer init, PoisonedByTiberiumInfo info)
			: base(info)
		{
			rl = init.Self.World.WorldActor.Trait<ResourceLayer>();
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled || --poisonTicks > 0)
				return;

			// Prevents harming infantry in cargo.
			if (!self.IsInWorld)
				return;

			var r = rl.GetResource(self.Location);
			if (r == null || !Info.Resources.Contains(r.Info.Name))
				return;

			Info.WeaponInfo.Impact(Target.FromActor(self), self.World.WorldActor, Enumerable.Empty<int>());
			poisonTicks = Info.WeaponInfo.ReloadDelay;
		}
	}
}
