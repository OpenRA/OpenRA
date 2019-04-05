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
	public class FindAndDeliverResources : Activity
	{
		readonly Harvester harv;
		readonly HarvesterInfo harvInfo;
		readonly Mobile mobile;
		readonly LocomotorInfo locomotorInfo;
		readonly ResourceClaimLayer claimLayer;
		readonly IPathFinder pathFinder;
		readonly DomainIndex domainIndex;
		readonly Actor deliverActor;

		CPos? orderLocation;
		CPos? lastHarvestedCell;
		bool hasDeliveredLoad;
		bool hasHarvestedCell;
		bool hasWaited;

		public FindAndDeliverResources(Actor self, Actor deliverActor = null)
		{
			harv = self.Trait<Harvester>();
			harvInfo = self.Info.TraitInfo<HarvesterInfo>();
			mobile = self.Trait<Mobile>();
			locomotorInfo = mobile.Info.LocomotorInfo;
			claimLayer = self.World.WorldActor.Trait<ResourceClaimLayer>();
			pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();
			this.deliverActor = deliverActor;
		}

		public FindAndDeliverResources(Actor self, CPos orderLocation)
			: this(self, null)
		{
			this.orderLocation = orderLocation;
		}

		protected override void OnFirstRun(Actor self)
		{
			// If an explicit "harvest" order is given, direct the harvester to the ordered location instead of
			// the previous harvested cell for the initial search.
			if (orderLocation != null)
			{
				lastHarvestedCell = orderLocation;

				// If two "harvest" orders are issued consecutively, we deliver the load first if needed.
				// We have to make sure the actual "harvest" order is not skipped if a third order is queued,
				// so we keep deliveredLoad false.
				if (harv.IsFull)
					QueueChild(self, new DeliverResources(self), true);
			}

			// If an explicit "deliver" order is given, the harvester goes immediately to the refinery.
			if (deliverActor != null)
			{
				QueueChild(self, new DeliverResources(self, deliverActor), true);
				hasDeliveredLoad = true;
			}
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (IsCanceling)
				return NextActivity;

			if (NextActivity != null)
			{
				// Interrupt automated harvesting after clearing the first cell.
				if (!harvInfo.QueueFullLoad && (hasHarvestedCell || harv.LastSearchFailed))
					return NextActivity;

				// Interrupt automated harvesting after first complete harvest cycle.
				if (hasDeliveredLoad || harv.IsFull)
					return NextActivity;
			}

			// Are we full or have nothing more to gather? Deliver resources.
			if (harv.IsFull || (!harv.IsEmpty && harv.LastSearchFailed))
			{
				QueueChild(self, new DeliverResources(self), true);
				hasDeliveredLoad = true;
				return this;
			}

			// After a failed search, wait and sit still for a bit before searching again.
			if (harv.LastSearchFailed && !hasWaited)
			{
				QueueChild(self, new Wait(harv.Info.WaitDuration), true);
				hasWaited = true;
				return this;
			}

			hasWaited = false;

			// Scan for resources. If no resources are found near the current field, search near the refinery
			// instead. If that doesn't help, give up for now.
			var closestHarvestableCell = ClosestHarvestablePos(self);
			if (!closestHarvestableCell.HasValue)
			{
				if (lastHarvestedCell != null)
				{
					lastHarvestedCell = null; // Forces search from backup position.
					closestHarvestableCell = ClosestHarvestablePos(self);
					harv.LastSearchFailed = !closestHarvestableCell.HasValue;
				}
				else
					harv.LastSearchFailed = true;
			}
			else
				harv.LastSearchFailed = false;

			// If no harvestable position could be found and we are at the refinery, get out of the way
			// of the refinery entrance.
			if (harv.LastSearchFailed)
			{
				var lastproc = harv.LastLinkedProc ?? harv.LinkedProc;
				if (lastproc != null && !lastproc.Disposed)
				{
					var deliveryLoc = lastproc.Location + lastproc.Trait<IAcceptResources>().DeliveryOffset;
					if (self.Location == deliveryLoc && harv.IsEmpty)
					{
						var unblockCell = deliveryLoc + harv.Info.UnblockCell;
						var moveTo = mobile.NearestMoveableCell(unblockCell, 1, 5);
						self.SetTargetLine(Target.FromCell(self.World, moveTo), Color.Green, false);
						QueueChild(self, mobile.MoveTo(moveTo, 1), true);
					}
				}

				return this;
			}

			// If we get here, our search for resources was successful. Commence harvesting.
			QueueChild(self, new HarvestResource(self, closestHarvestableCell.Value), true);
			lastHarvestedCell = closestHarvestableCell.Value;
			hasHarvestedCell = true;
			return this;
		}

		/// <summary>
		/// Finds the closest harvestable pos between the current position of the harvester
		/// and the last order location
		/// </summary>
		CPos? ClosestHarvestablePos(Actor self)
		{
			// Harvesters should respect an explicit harvest order instead of harvesting the current cell.
			if (orderLocation == null)
			{
				if (harv.CanHarvestCell(self, self.Location) && claimLayer.CanClaimCell(self, self.Location))
					return self.Location;
			}
			else
			{
				if (harv.CanHarvestCell(self, orderLocation.Value) && claimLayer.CanClaimCell(self, orderLocation.Value))
					return orderLocation;

				orderLocation = null;
			}

			// Determine where to search from and how far to search:
			var searchFromLoc = lastHarvestedCell ?? GetSearchFromLocation(self);
			var searchRadius = lastHarvestedCell.HasValue ? harvInfo.SearchFromOrderRadius : harvInfo.SearchFromProcRadius;
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
			if (harv.LastLinkedProc != null && !harv.LastLinkedProc.IsDead && harv.LastLinkedProc.IsInWorld)
				return harv.LastLinkedProc.Location + harv.LastLinkedProc.Trait<IAcceptResources>().DeliveryOffset;

			if (harv.LinkedProc != null && !harv.LinkedProc.IsDead && harv.LinkedProc.IsInWorld)
				return harv.LinkedProc.Location + harv.LinkedProc.Trait<IAcceptResources>().DeliveryOffset;

			return self.Location;
		}
	}
}
