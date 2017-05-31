#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HarvestResource : Activity
	{
		readonly Harvester harv;
		readonly HarvesterInfo harvInfo;
		readonly IFacing facing;
		readonly ResourceClaimLayer territory;
		readonly ResourceLayer resLayer;
		readonly BodyOrientation body;

		public HarvestResource(Actor self)
		{
			harv = self.Trait<Harvester>();
			harvInfo = self.Info.TraitInfo<HarvesterInfo>();
			facing = self.Trait<IFacing>();
			body = self.Trait<BodyOrientation>();
			territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
			resLayer = self.World.WorldActor.Trait<ResourceLayer>();
		}

		Activity UnclaimAndNext(Actor self)
		{
			if (territory != null)
				territory.UnclaimByActor(self);
			return NextActivity;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return UnclaimAndNext(self);

			if (!self.CanHarvestAt(self.Location, resLayer, harvInfo, territory))
			{
				Queue(new FindResources(self));
				return UnclaimAndNext(self);
			}

			harv.LastHarvestedCell = self.Location;

			if (harv.IsFull)
				return UnclaimAndNext(self);

			// Turn to one of the harvestable facings
			if (harvInfo.HarvestFacings != 0)
			{
				var current = facing.Facing;
				var desired = body.QuantizeFacing(current, harvInfo.HarvestFacings);
				if (desired != current)
				{
					return ActivityUtils.SequenceActivities(new Turn(self, desired), this);
				}
			}

			var resource = resLayer.Harvest(self.Location);
			if (resource == null)
			{
				Queue(new FindResources(self));
				return UnclaimAndNext(self);
			}

			harv.AcceptResource(resource);

			foreach (var t in self.TraitsImplementing<INotifyHarvesterAction>())
				t.Harvested(self, resource);

			Queue(new Wait(harvInfo.BaleLoadDelay));
			Queue(new HarvestResource(self));
			return NextActivity;
		}
	}
}
