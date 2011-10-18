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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ChronoshiftableInfo : TraitInfo<Chronoshiftable> { }

	public class Chronoshiftable : ITick, ISync
	{
		// Return-to-sender logic
		[Sync]
		int2 chronoshiftOrigin;
		[Sync]
		int chronoshiftReturnTicks = 0;

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
				// Todo: need a new Teleport method that will move to the closest available cell
				self.QueueActivity(new Teleport(chronoshiftOrigin));
			}
		}

		// Can't be used in synced code, except with ignoreVis.
		public virtual bool CanChronoshiftTo(Actor self, int2 targetLocation, bool ignoreVis)
		{
			// Todo: Allow enemy units to be chronoshifted into bad terrain to kill them
			return self.HasTrait<ITeleportable>() &&
				self.Trait<ITeleportable>().CanEnterCell(targetLocation) &&
				(ignoreVis || self.World.LocalShroud.IsExplored(targetLocation));
		}

		public virtual bool Teleport(Actor self, int2 targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			/// Set up return-to-sender info
			chronoshiftOrigin = self.Location;
			chronoshiftReturnTicks = duration;

			// Kill cargo
			if (killCargo && self.HasTrait<Cargo>())
			{
				var cargo = self.Trait<Cargo>();
				while (!cargo.IsEmpty(self))
				{
					chronosphere.Owner.Kills++;
					var a = cargo.Unload(self);
					a.Owner.Deaths++;
				}
			}

			// Set up the teleport
			self.CancelActivity();
			self.QueueActivity(new Teleport(targetLocation));

			return true;
		}
	}
}
