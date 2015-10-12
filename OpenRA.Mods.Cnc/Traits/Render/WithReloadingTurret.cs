#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Renders ammo-dependent turret graphics for units with the Turreted trait.")]
	public class WithReloadingTurretInfo : WithTurretInfo, Requires<AmmoPoolInfo>, Requires<ArmamentInfo>
	{
		[Desc("AmmoPool to use for ammo-dependent sequences.")]
		public readonly string AmmoPoolName = null;

		[Desc("How many reload stages does this turret have. Defaults to AmmoPool's Ammo.",
			"Adds current reload stage to Sequence as suffix when a matching AmmoPool is present.")]
		public readonly int ReloadStages = -1;

		public override object Create(ActorInitializer init) { return new WithReloadingTurret(init.Self, this); }
	}

	public class WithReloadingTurret : WithTurret
	{
		readonly int reloadStages;
		readonly AmmoPool ammoPool;
		string sequence;
		string ammoSuffix;

		public WithReloadingTurret(Actor self, WithReloadingTurretInfo info)
			: base(self, info)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>().FirstOrDefault(a => a.Info.Name == info.AmmoPoolName);
			if (ammoPool == null)
				throw new InvalidOperationException("Actor type '" + self.Info.Name + "' does not define a valid ammo pool for its reloading turret.");

			sequence = Info.Sequence;
			reloadStages = info.ReloadStages;

			var initialAmmo = ammoPool.Info.InitialAmmo;
			var ammo = ammoPool.Info.Ammo;
			var initialAmmoStage = initialAmmo >= 0 && initialAmmo != ammo ? initialAmmo : ammo;

			if (ammoPool != null && reloadStages < 0)
				ammoSuffix = initialAmmoStage.ToString();
			if (ammoPool != null && reloadStages >= 0)
				ammoSuffix = (initialAmmoStage * reloadStages / ammo).ToString();
		}

		public override void Tick(Actor self)
		{
			if (Info.AimSequence != null)
				sequence = Attack.IsAttacking ? Info.AimSequence : Info.Sequence;

			var currentAmmo = ammoPool.GetAmmoCount();
			if (reloadStages < 0)
				ammoSuffix = currentAmmo.ToString();
			if (reloadStages >= 0)
				ammoSuffix = (currentAmmo * reloadStages / ammoPool.Info.Ammo).ToString();

			var newSequence = NormalizeSequence(self, sequence + ammoSuffix);
			if (DefaultAnimation.CurrentSequence.Name != newSequence)
				DefaultAnimation.ReplaceAnim(newSequence);
		}
	}
}
