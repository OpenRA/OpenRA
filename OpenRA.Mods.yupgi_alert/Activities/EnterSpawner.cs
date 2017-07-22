#region Copyright & License Information
/*
 * Modded by Boolbada of OP mod, from Engineer repair enter activity.
 * 
 * Note: You can still use this without modifying the OpenRA engine itself by deleting 
 * FindAndTransitionToNextState. I just deleted a few lines of "movement" recovery code so that
 * interceptors can enter moving carrier.
 * However, for better results, consider modding the engine, as in the following commit:
 * https://github.com/forcecore/OpenRA/commit/fd36f63e508b7ad28e7d320355b7d257654b33ee
 * 
 * Also, interceptors sometimes try to land on ground level.
 * To mitigate that, I added LnadingDistance in Spawned trait.
 * However, that isn't perfect. For perfect results, Land.cs of the engine must be modified:
 * https://github.com/forcecore/OpenRA/commit/45970f57283150bc57ce86b8ce8a555018c6ca14
 * I couldn't make it independent as it relies on other stuff in Enter.cs too much.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Yupgi_alert.Traits;
using OpenRA.Traits;

/*
Requires base engine changes.
Since this inherits "Enter", you need to make several variables "protected".
*/

namespace OpenRA.Mods.Yupgi_alert.Activities
{
	class EnterSpawner : Enter
	{
		readonly Actor master; // remember spawner.
		readonly Spawner spawner;
		readonly AmmoPool[] ammoPools;
		//// readonly Dictionary<AmmoPool, int> ammoPoolsReloadTimes;

		public EnterSpawner(Actor self, Actor target, EnterBehaviour enterBehaviour, WDist closeEnoughDist)
			: base(self, target, enterBehaviour, closeEnoughDist)
		{
			this.master = target;
			spawner = target.Trait<Spawner>();

			ammoPools = self.TraitsImplementing<AmmoPool>().Where(p => !p.Info.SelfReloads).ToArray();

			if (ammoPools == null)
				return;
		}

		protected override bool CanReserve(Actor self)
		{
			return true; // Slaves are always welcome.
		}

		protected override ReserveStatus Reserve(Actor self)
		{
			// TryReserveElseTryAlternateReserve calls Reserve and
			// the default inplementation of Reserve() returns TooFar when
			// the aircraft carrier is hovering over a building.
			// Since spawners don't need reservation (and have no reservation trait) just return Ready
			// so that spawner can enter no matter where the spawner is.
			return ReserveStatus.Ready;
		}

		protected override void OnInside(Actor self)
		{
			// Master got killed :(
			if (master.IsDead)
				return;

			Done(self); // Stop slaves from exiting.

			// Load this thingy.
			// Issue attack move to the rally point.
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || master.IsDead || !spawner.CanLoad(master, self))
					return;

				spawner.Load(master, self);
				w.Remove(self);

				// Insta repair.
				var info = master.Info.TraitInfo<SpawnerInfo>();
				if (info.InstaRepair)
				{
					var health = self.Trait<Health>();
					self.InflictDamage(self, new Damage(-health.MaxHP));
				}

				// Insta re-arm. (Delayed launching is handled at spawner.)
				foreach (var pool in ammoPools)
				{
					while (pool.GiveAmmo()); // fill 'er up.
				}
			});
		}
	}
}
