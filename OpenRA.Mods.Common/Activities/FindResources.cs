#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FindResources : Activity
	{
		readonly Harvester harv;
		readonly HarvesterInfo harvInfo;
		readonly Mobile mobile;
		readonly LocomotorInfo locomotorInfo;
		readonly ResourceClaimLayer claimLayer;
		readonly IPathFinder pathFinder;
		readonly DomainIndex domainIndex;

		CPos? orderLocation;

		public FindResources(Actor self)
		{
			harv = self.Trait<Harvester>();
			harvInfo = self.Info.TraitInfo<HarvesterInfo>();
			mobile = self.Trait<Mobile>();
			locomotorInfo = mobile.Info.LocomotorInfo;
			claimLayer = self.World.WorldActor.Trait<ResourceClaimLayer>();
			pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();
		}

		public FindResources(Actor self, CPos orderLocation)
			: this(self)
		{
			this.orderLocation = orderLocation;
		}

		protected override void OnFirstRun(Actor self)
		{
			// Without this, multiple "Harvest" orders queued directly after each other
			// will be skipped because the harvester starts off full.
			if (harv.IsFull)
				QueueChild(self, new DeliverResources(self));
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			if (IsCanceling)
				return NextActivity;

			if (harv.IsFull)
			{
				return NextActivity;
			}

			// If an explicit order is given, direct the harvester to the ordered location instead of the previous
			// harvested cell for the initial search.
			if (orderLocation != null)
			{
				harv.LastHarvestedCell = orderLocation;
				orderLocation = null;
			}

			var closestHarvestablePosition = ClosestHarvestablePos(self);

			// If no harvestable position could be found, either deliver the remaining resources
			// or get out of the way and do not disturb.
			if (!closestHarvestablePosition.HasValue)
			{
				// If no resources are found near the current field, search near the refinery instead.
				// If that doesn't help, give up for now.
				if (harv.LastHarvestedCell != null)
					harv.LastHarvestedCell = null;
				else
					harv.LastSearchFailed = true;

				var lastproc = harv.LastLinkedProc ?? harv.LinkedProc;
				if (lastproc != null && !lastproc.Disposed)
				{
					var deliveryLoc = lastproc.Location + lastproc.Trait<IAcceptResources>().DeliveryOffset;
					if (self.Location == deliveryLoc && harv.IsEmpty)
					{
						// Get out of the way:
						var unblockCell = deliveryLoc + harv.Info.UnblockCell;
						var moveTo = mobile.NearestMoveableCell(unblockCell, 1, 5);
						self.SetTargetLine(Target.FromCell(self.World, moveTo), Color.Green, false);
						QueueChild(self, mobile.MoveTo(moveTo, 1), true);
						return this;
					}
				}

				return NextActivity;
			}

			// Attempt to claim the target cell
			if (!claimLayer.TryClaimCell(self, closestHarvestablePosition.Value))
			{
				QueueChild(self, new Wait(25), true);
				return this;
			}

			harv.LastSearchFailed = false;

			foreach (var n in self.TraitsImplementing<INotifyHarvesterAction>())
				n.MovingToResources(self, closestHarvestablePosition.Value, new FindResources(self));

			self.SetTargetLine(Target.FromCell(self.World, closestHarvestablePosition.Value), Color.Red, false);
			QueueChild(self, mobile.MoveTo(closestHarvestablePosition.Value, 1), true);
			QueueChild(self, new HarvestResource(self));
			return this;
		}

		/// <summary>
		/// Finds the closest harvestable pos between the current position of the harvester
		/// and the last order location
		/// </summary>
		CPos? ClosestHarvestablePos(Actor self)
		{
			if (harv.CanHarvestCell(self, self.Location) && claimLayer.CanClaimCell(self, self.Location))
				return self.Location;

			// Determine where to search from and how far to search:
			var searchFromLoc = harv.LastHarvestedCell ?? GetSearchFromLocation(self);
			var searchRadius = harv.LastHarvestedCell.HasValue ? harvInfo.SearchFromOrderRadius : harvInfo.SearchFromProcRadius;
			var searchRadiusSquared = searchRadius * searchRadius;

			// Find any harvestable resources:
			List<CPos> path;
			using (var search = PathSearch.Search(self.World, locomotorInfo, self, true, loc =>
					domainIndex.IsPassable(self.Location, loc, locomotorInfo) && harv.CanHarvestCell(self, loc) && claimLayer.CanClaimCell(self, loc))
				.WithCustomCost(loc =>
				{
					if ((loc - searchFromLoc).LengthSquared > searchRadiusSquared)
						return int.MaxValue;

					return 0;
				})
				.FromPoint(searchFromLoc)
				.FromPoint(self.Location))
				path = pathFinder.FindPath(search);

			if (path.Count > 0)
				return path[0];

			return null;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromCell(self.World, self.Location);
		}

		CPos GetSearchFromLocation(Actor self)
		{
			if (harv.LastLinkedProc != null)
				return harv.LastLinkedProc.Location + harv.LastLinkedProc.Trait<IAcceptResources>().DeliveryOffset;

			if (harv.LinkedProc != null)
				return harv.LinkedProc.Location + harv.LinkedProc.Trait<IAcceptResources>().DeliveryOffset;

			return self.Location;
		}
	}
}
