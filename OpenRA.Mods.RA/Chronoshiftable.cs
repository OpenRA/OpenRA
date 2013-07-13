#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ChronoshiftableInfo : ITraitInfo
	{
		public readonly bool ExplodeInstead = false;
		public readonly string ChronoshiftSound = "chrono2.aud";

		public object Create(ActorInitializer init) { return new Chronoshiftable(this); }
	}

	public class Chronoshiftable : ITick, ISync, ISelectionBar
	{
		// Return-to-sender logic
		[Sync] CPos chronoshiftOrigin;
		[Sync] int chronoshiftReturnTicks = 0;
		Actor chronosphere;
		bool killCargo;
		int TotalTicks;
		readonly ChronoshiftableInfo info;

		public Chronoshiftable(ChronoshiftableInfo info)
		{
			this.info = info;
		}

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
				// TODO: need a new Teleport method that will move to the closest available cell
				self.QueueActivity(new Teleport(chronosphere, chronoshiftOrigin, killCargo, info.ChronoshiftSound));
			}
		}

		// Can't be used in synced code, except with ignoreVis.
		public virtual bool CanChronoshiftTo(Actor self, CPos targetLocation)
		{
			// TODO: Allow enemy units to be chronoshifted into bad terrain to kill them
			return (self.HasTrait<ITeleportable>() && self.Trait<ITeleportable>().CanEnterCell(targetLocation));
		}

		public virtual bool Teleport(Actor self, CPos targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			if (info.ExplodeInstead)	// some things appear chronoshiftable, but instead they just die.
			{
				self.World.AddFrameEndTask(w =>
				{
					// damage is inflicted by the chronosphere
					if (!self.Destroyed) self.InflictDamage(chronosphere, int.MaxValue, null); 
				});
				return true;
			}

			/// Set up return-to-sender info
			chronoshiftOrigin = self.Location;
			chronoshiftReturnTicks = duration;
			TotalTicks = duration;
			this.chronosphere = chronosphere;
			this.killCargo = killCargo;

			// Set up the teleport
			self.CancelActivity();
			self.QueueActivity(new Teleport(chronosphere, targetLocation, killCargo, info.ChronoshiftSound));

			return true;
		}

		// Show the remaining time as a bar
		public float GetValue()
		{
			if (chronoshiftReturnTicks == 0) // otherwise an empty bar is rendered all the time
				return 0f;

			return (float)chronoshiftReturnTicks / TotalTicks;
		}
		public Color GetColor() { return Color.White; }
	}
}
