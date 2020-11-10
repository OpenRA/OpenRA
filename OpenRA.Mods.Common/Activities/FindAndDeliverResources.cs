#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
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
		readonly ResourceClaimLayer claimLayer;
		readonly IPathFinder pathFinder;
		readonly DomainIndex domainIndex;
		readonly CPos? orderLocation;

		Actor deliverActor;
		CPos? lastHarvestedCell;
		bool hasDeliveredLoad;
		bool hasHarvestedCell;
		bool hasWaited;

		public bool LastSearchFailed { get; private set; }

		public FindAndDeliverResources(Actor self, Actor deliverActor = null)
		{
			harv = self.Trait<Harvester>();
			harvInfo = self.Info.TraitInfo<HarvesterInfo>();
			mobile = self.Trait<Mobile>();
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
			// The NextActivity handling will drop this activity once the harvester is full,
			// so deliver the load first if needed while keeping 'hasDeliveredLoad' false.
			if (orderLocation != null && harv.IsFull)
				QueueChild(new DeliverResources(self));

			// If an explicit "deliver" order is given, the harvester goes immediately to the refinery.
			else if (deliverActor != null)
			{
				QueueChild(new DeliverResources(self, deliverActor));
				hasDeliveredLoad = true;
				deliverActor = null;
			}
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			if (NextActivity != null)
			{
				// Interrupt automated harvesting after clearing the first cell.
				if (!harvInfo.QueueFullLoad && (hasHarvestedCell || LastSearchFailed))
					return true;

				// Interrupt automated harvesting after first complete harvest cycle.
				if (hasDeliveredLoad || harv.IsFull)
					return true;
			}

			if (harv.IsFull)
			{
				QueueChild(new DeliverResources(self));
				hasDeliveredLoad = true;
				return false;
			}

			// After a failed search, wait and sit still for a bit before searching again.
			if (LastSearchFailed && !hasWaited)
			{
				QueueChild(new Wait(harv.Info.WaitDuration));
				hasWaited = true;
				return false;
			}

			hasWaited = false;

			// Scan for resources. If no resources are found give up for now.
			var closestHarvestableCell = ClosestHarvestablePos(self);
			LastSearchFailed = !closestHarvestableCell.HasValue;

			if (LastSearchFailed)
			{
				// Deliver resources first if we can. A new harvestable pos might be available once we are finished.
				if (!harv.IsEmpty)
				{
					QueueChild(new DeliverResources(self));
					hasDeliveredLoad = true;
					hasWaited = true;
					return false;
				}

				// If no harvestable position could be found and we are at the refinery,
				// get out of the way of the refinery entrance.
				var lastproc = harv.LastLinkedProc ?? harv.LinkedProc;
				if (lastproc != null && !lastproc.Disposed)
				{
					var deliveryLoc = lastproc.Location + lastproc.Trait<IAcceptResources>().DeliveryOffset;
					if (self.Location == deliveryLoc && harv.IsEmpty)
					{
						var unblockCell = deliveryLoc + harv.Info.UnblockCell;
						var moveTo = mobile.NearestMoveableCell(unblockCell, 1, 5);
						QueueChild(mobile.MoveTo(moveTo, 1));
					}
				}

				return false;
			}

			// If we get here, our search for resources was successful. Commence harvesting.
			QueueChild(new HarvestResource(self, closestHarvestableCell.Value));
			lastHarvestedCell = closestHarvestableCell.Value;
			hasHarvestedCell = true;
			return false;
		}

		/// <summary>
		/// If no explicit order is given, the current location is returned if it is harvestable.
		/// If an explicit order is given, the order location is returned if it is harvestable.
		/// Otherwise finds the closest harvestable position searching in order from:
		/// - Location set by an explicit order with <see cref="HarvesterInfo.SearchFromHarvesterRadius"/>.
		/// - Location of the (last) linked refinery with <see cref="HarvesterInfo.SearchFromProcRadius"/>.
		/// - Location last harvested at with <see cref="HarvesterInfo.SearchFromHarvesterRadius"/>.
		/// - The harvester's current location with <see cref="HarvesterInfo.SearchFromHarvesterRadius"/>.
		/// </summary>
		CPos? ClosestHarvestablePos(Actor self)
		{
			// Harvesters should respect an explicit harvest order instead of harvesting the current cell.
			if (orderLocation == null && harv.CanHarvestCell(self, self.Location) && claimLayer.CanClaimCell(self, self.Location))
				return self.Location;

			// Directly start harvesting at the explicit location if we can
			if (orderLocation != null && harv.CanHarvestCell(self, orderLocation.Value) && claimLayer.CanClaimCell(self, orderLocation.Value))
				return orderLocation;

			var procLoc = GetSearchFromProcLocation(self);
			if (orderLocation.HasValue)
			{
				var closest = PathSearchHarvestablePos(self, orderLocation.Value, harvInfo.SearchFromHarvesterRadius, procLoc);
				if (closest.HasValue)
					return closest;
			}

			if (procLoc.HasValue)
			{
				var closest = PathSearchHarvestablePos(self, procLoc.Value, harvInfo.SearchFromProcRadius, procLoc);
				if (closest.HasValue)
					return closest;
			}

			if (lastHarvestedCell.HasValue)
			{
				var closest = PathSearchHarvestablePos(self, lastHarvestedCell.Value, harvInfo.SearchFromHarvesterRadius, procLoc);
				if (closest.HasValue)
					return closest;
			}

			return PathSearchHarvestablePos(self, self.Location, harvInfo.SearchFromHarvesterRadius, procLoc);
		}

		CPos? PathSearchHarvestablePos(Actor self, CPos searchFromLoc, int searchRadius, CPos? procLoc)
		{
			var searchRadiusSquared = searchRadius * searchRadius;
			var procPos = procLoc.HasValue ? (WPos?)self.World.Map.CenterOfCell(procLoc.Value) : null;
			var harvPos = self.CenterPosition;

			// Find any harvestable resources:
			List<CPos> path;
			using (var search = PathSearch.Search(self.World, mobile.Locomotor, self, BlockedByActor.Stationary, loc =>
					domainIndex.IsPassable(self.Location, loc, mobile.Locomotor) && harv.CanHarvestCell(self, loc) && claimLayer.CanClaimCell(self, loc))
				.WithCustomCost(loc =>
				{
					if ((loc - searchFromLoc).LengthSquared > searchRadiusSquared)
						return int.MaxValue;

					// Add a cost modifier to harvestable cells to prefer resources that are closer to the refinery.
					// This reduces the tendancy for harvesters to move in straight lines
					if (procPos.HasValue && harvInfo.ResourceRefineryDirectionPenalty > 0 && harv.CanHarvestCell(self, loc))
					{
						var pos = self.World.Map.CenterOfCell(loc);

						// Calculate harv-cell-refinery angle (cosine rule)
						var a = harvPos - procPos.Value;
						var b = pos - procPos.Value;
						var c = pos - harvPos;

						if (b != WVec.Zero && c != WVec.Zero)
						{
							var cosA = (int)(512 * (b.LengthSquared + c.LengthSquared - a.LengthSquared) / b.Length / c.Length);

							// Cost modifier varies between 0 and ResourceRefineryDirectionPenalty
							return Math.Abs(harvInfo.ResourceRefineryDirectionPenalty / 2) + harvInfo.ResourceRefineryDirectionPenalty * cosA / 2048;
						}
					}

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

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity != null)
				foreach (var n in ChildActivity.TargetLineNodes(self))
					yield return n;
			else if (orderLocation != null)
				yield return new TargetLineNode(Target.FromCell(self.World, orderLocation.Value), Color.Crimson);
			else if (deliverActor != null)
				yield return new TargetLineNode(Target.FromActor(deliverActor), Color.Green);
		}

		CPos? GetSearchFromProcLocation(Actor self)
		{
			if (harv.LastLinkedProc != null && !harv.LastLinkedProc.IsDead && harv.LastLinkedProc.IsInWorld)
				return harv.LastLinkedProc.Location + harv.LastLinkedProc.Trait<IAcceptResources>().DeliveryOffset;

			if (harv.LinkedProc != null && !harv.LinkedProc.IsDead && harv.LinkedProc.IsInWorld)
				return harv.LinkedProc.Location + harv.LinkedProc.Trait<IAcceptResources>().DeliveryOffset;

			return null;
		}
	}
}
