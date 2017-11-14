#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Rearm : Activity
	{
		readonly Target host;
		readonly WDist closeEnough;
		readonly AmmoPool[] ammoPools;

		public Rearm(Actor self, Actor host, WDist closeEnough)
		{
			this.host = Target.FromActor(host);
			this.closeEnough = closeEnough;
			ammoPools = self.TraitsImplementing<AmmoPool>().Where(p => !p.AutoReloads).ToArray();
		}

		protected override void OnFirstRun(Actor self)
		{
			// Reset the ReloadDelay to avoid any issues with early cancellation
			// from previous reload attempts (explicit order, host building died, etc).
			// HACK: this really shouldn't be managed from here
			foreach (var pool in ammoPools)
				pool.RemainingTicks = pool.Info.ReloadDelay;

			if (host.Type == TargetType.Invalid)
				return;

			foreach (var notify in host.Actor.TraitsImplementing<INotifyRearm>())
				notify.RearmingStarted(host.Actor, self);
		}

		protected override void OnLastRun(Actor self)
		{
			if (host.Type == TargetType.Invalid)
				return;

			foreach (var notify in host.Actor.TraitsImplementing<INotifyRearm>())
				notify.RearmingFinished(host.Actor, self);
		}

		protected override void OnActorDispose(Actor self)
		{
			// If the actor died (or will be disposed directly) this tick, Activity.TickOuter won't be ticked again,
			// so we need to run OnLastRun directly (otherwise it would be skipped completely).
			OnLastRun(self);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if (host.Type == TargetType.Invalid)
				return NextActivity;

			if (closeEnough.LengthSquared > 0 && !host.IsInRange(self.CenterPosition, closeEnough))
				return NextActivity;

			var complete = true;
			foreach (var pool in ammoPools)
			{
				if (!pool.FullAmmo())
				{
					Reload(self, host.Actor, pool);
					complete = false;
				}
			}

			return complete ? NextActivity : this;
		}

		void Reload(Actor self, Actor host, AmmoPool ammoPool)
		{
			if (--ammoPool.RemainingTicks <= 0)
			{
				foreach (var notify in host.TraitsImplementing<INotifyRearm>())
					notify.Rearming(host, self);

				ammoPool.RemainingTicks = ammoPool.Info.ReloadDelay;
				if (!string.IsNullOrEmpty(ammoPool.Info.RearmSound))
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, ammoPool.Info.RearmSound, self.CenterPosition);

				ammoPool.GiveAmmo(self, ammoPool.Info.ReloadCount);
			}
		}
	}
}
