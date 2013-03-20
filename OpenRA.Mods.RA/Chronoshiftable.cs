#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */



#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ChronoshiftableInfo : TraitInfo<Chronoshiftable>
	{
		public readonly bool ExplodeInstead = false;
	}

	public class Chronoshiftable : ITick, ISync
	{
		// Return-to-sender logic
		[Sync] CPos chronoshiftOrigin;
		[Sync] int chronoshiftReturnTicks = 0;
		Actor chronosphere;
		bool killCargo;

		public void Tick(Actor self)
		{
			if (chronoshiftReturnTicks <= 0)
				return;

			if (chronoshiftReturnTicks > 0)
				chronoshiftReturnTicks--;

			// Return to original location
			if (chronoshiftReturnTicks == 0)
			{
				self.CancelActivity();
				stopAttackers(self);
				// Todo: need a new Teleport method that will move to the closest available cell
				self.QueueActivity(new Teleport(chronosphere, chronoshiftOrigin, killCargo));
			}
		}

		protected void stopAttackers(Actor self)
		{
			foreach (Actor a in self.World.Actors)
			{
				if (a.GetCurrentActivity() is Attack)
				{
					var attack = (Attack) a.GetCurrentActivity();
					if (attack.Target.IsActor && attack.Target.Actor == self)
						attack.Cancel(a);
				}
				else if (a.GetCurrentActivity() is FlyAttack)
				{
					var attack = (FlyAttack) a.GetCurrentActivity();
					if (attack.Target.IsActor && attack.Target.Actor == self)
						attack.Cancel(a);
				}
				else if (a.GetCurrentActivity() is HeliAttack)
				{
					var attack = (HeliAttack) a.GetCurrentActivity();
					if (attack.Target.IsActor && attack.Target.Actor == self)
						attack.Cancel(a);
				}
			}
		}

		// Can't be used in synced code, except with ignoreVis.
		public virtual bool CanChronoshiftTo(Actor self, CPos targetLocation)
		{
			// Todo: Allow enemy units to be chronoshifted into bad terrain to kill them
			return (self.HasTrait<ITeleportable>() && self.Trait<ITeleportable>().CanEnterCell(targetLocation));
		}

		public virtual bool Teleport(Actor self, CPos targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			var info = self.Info.Traits.Get<ChronoshiftableInfo>();
			if (info.ExplodeInstead)	// some things appear chronoshiftable, but instead they just die.
			{
				self.World.AddFrameEndTask(w =>
				{
					// damage is inflicted by the chronosphere
					if (!self.Destroyed) self.InflictDamage(chronosphere, int.MaxValue, null); 
				});
				return true;
			}

			stopAttackers(self);

			/// Set up return-to-sender info
			chronoshiftOrigin = self.Location;
			chronoshiftReturnTicks = duration;
			this.chronosphere = chronosphere;
			this.killCargo = killCargo;

			// Set up the teleport
			self.CancelActivity();
			self.QueueActivity(new Teleport(chronosphere, targetLocation, killCargo));

			return true;
		}
	}
}
