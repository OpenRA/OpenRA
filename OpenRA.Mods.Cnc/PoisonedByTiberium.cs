#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Mods.RA;
using OpenRA.GameRules;

namespace OpenRA.Mods.Cnc
{
	class PoisonedByTiberiumInfo : ITraitInfo
	{
		[WeaponReference] public readonly string Weapon = "Tiberium";
		public readonly string[] Resources = { "Tiberium", "BlueTiberium" };

		public object Create(ActorInitializer init) { return new PoisonedByTiberium(this); }
	}

	class PoisonedByTiberium : ITick, ISync
	{
		PoisonedByTiberiumInfo info;
		[Sync] int poisonTicks;

		public PoisonedByTiberium(PoisonedByTiberiumInfo info) { this.info = info; }

		public void Tick(Actor self)
		{
			if (--poisonTicks > 0) return;

			// Prevents harming infantry in cargo.
			if (!self.Flagged(ActorFlag.InWorld)) return;

			var rl = self.World.WorldActor.Trait<ResourceLayer>();
			var r = rl.GetResource(self.Location);
			if (r == null) return;
			if (!info.Resources.Contains(r.Info.Name)) return;

			var weapon = self.World.Map.Rules.Weapons[info.Weapon.ToLowerInvariant()];

			weapon.Impact(Target.FromActor(self), self.World.WorldActor, Enumerable.Empty<int>());
			poisonTicks = weapon.ReloadDelay;
		}
	}
}
