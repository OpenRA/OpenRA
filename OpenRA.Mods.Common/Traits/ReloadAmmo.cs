#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	// TODO: Add InitializeAfter<ReloadAmmoInfo> to AmmoPool once possible, and update description accordingly.
	[Desc("Reloads ammo pools.Put this before AmmoPool in yaml rules.")]
	public class ReloadAmmoInfo : UpgradableTraitInfo
	{
		[Desc("Reload ammo pools with these names.")]
		public readonly HashSet<string> ReloadAmmoPools = new HashSet<string> { "primary" };

		[Desc("Reload time in ticks per AmmoPool.ReloadCount.")]
		public readonly int ReloadTicks = 50;

		[Desc("Whether or not reload timer should be reset when ammo has been fired.")]
		public readonly bool ResetOnFire = false;

		public override object Create(ActorInitializer init) { return new ReloadAmmo(this); }
	}

	public class ReloadAmmo : UpgradableTrait<ReloadAmmoInfo>
	{
		public ReloadAmmo(ReloadAmmoInfo info)
			: base(info) { }
	}
}