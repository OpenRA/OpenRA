#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FindAndDeliverResources : Activity
	{
		readonly Harvester harv;
		readonly HarvesterInfo harvInfo;
		readonly Mobile mobile;
		readonly ResourceClaimLayer claimLayer;
		CPos? orderLocation;
		CPos? lastHarvestedCell;
		bool hasDeliveredLoad;
		bool hasHarvestedCell;
		bool hasWaited;

		public bool LastSearchFailed { get; private set; }

		public FindAndDeliverResources(Actor self, CPos? orderLocation = null)
		{
			harv = self.Trait<Harvester>();
			harvInfo = self.Info.TraitInfo<HarvesterInfo>();
			mobile = self.Trait<Mobile>();
			claimLayer = self.World.WorldActor.Trait<ResourceClaimLayer>();
			if (orderLocation.HasValue)
				this.orderLocation = orderLocation.Value;
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
					QueueChild(new MoveToDock(self));
			}
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || harv.IsTraitDisabled)
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

			// Are we full or have nothing more to gather? Deliver resources.
			if (harv.IsFull || (!harv.IsEmpty && LastSearchFailed))
			{
				// If we are reserved it means docking was already initiated and we should wait.
				if (harv.DockClientManager.ReservedHost != null)
					return false;

				QueueChild(new MoveToDock(self));
				hasDeliveredLoad = true;
			}

			// After a failed search, wait and sit still for a bit before searching again.
			if (LastSearchFailed && !hasWaited)
			{
				QueueChild(new Wait(harv.Info.WaitDuration));
				hasWaited = true;
				return false;
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
					LastSearchFailed = !closestHarvestableCell.HasValue;
				}
				else
					LastSearchFailed = true;
			}
			else
				LastSearchFailed = false;

			// If no harvestable position could be found and we are at the refinery, get out of the way
			// of the refinery entrance.
			if (LastSearchFailed)
			{
				var lastproc = harv.DockClientManager.LastReservedHost;
				if (lastproc != null)
				{
					var deliveryLoc = self.World.Map.CellContaining(lastproc.DockPosition);
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
		/// Finds the closest harvestable pos between the current position of the harvester
		/// and the last order location.
		/// </summary>
		CPos? ClosestHarvestablePos(Actor self)
		{
			// Harvesters should respect an explicit harvest order instead of harvesting the current cell.
			if (orderLocation == null)
			{
				if (harv.CanHarvestCell(self.Location) && claimLayer.CanClaimCell(self, self.Location))
					return self.Location;
			}
			else
			{
				if (harv.CanHarvestCell(orderLocation.Value) && claimLayer.CanClaimCell(self, orderLocation.Value))
					return orderLocation;

				orderLocation = null;
			}

			// Determine where to search from and how far to search:
			// Prioritise search by these locations in this order: lastHarvestedCell -> lastLinkedDock -> self.
			CPos searchFromLoc;
			int searchRadius;
			WPos? dockPos = null;
			if (lastHarvestedCell.HasValue)
			{
				searchRadius = harvInfo.SearchFromHarvesterRadius;
				searchFromLoc = lastHarvestedCell.Value;
			}
			else
			{
				searchRadius = harvInfo.SearchFromProcRadius;
				var dock = harv.DockClientManager.LastReservedHost;
				if (dock != null)
				{
					dockPos = dock.DockPosition;
					searchFromLoc = self.World.Map.CellContaining(dockPos.Value);
				}
				else
					searchFromLoc = self.Location;
			}

			var searchRadiusSquared = searchRadius * searchRadius;

			var map = self.World.Map;
			var harvPos = self.CenterPosition;

			// Find any harvestable resources:
			var path = mobile.PathFinder.FindPathToTargetCellByPredicate(
				self,
				new[] { searchFromLoc, self.Location },
				loc =>
					harv.CanHarvestCell(loc) &&
					claimLayer.CanClaimCell(self, loc),
				BlockedByActor.Stationary,
				loc =>
				{
					if ((loc - searchFromLoc).LengthSquared > searchRadiusSquared)
						return PathGraph.PathCostForInvalidPath;

					// Add a cost modifier to harvestable cells to prefer resources that are closer to the refinery.
					// This reduces the tendency for harvesters to move in straight lines
					if (dockPos.HasValue && harvInfo.ResourceRefineryDirectionPenalty > 0 && harv.CanHarvestCell(loc))
					{
						var pos = map.CenterOfCell(loc);

						// Calculate harv-cell-refinery angle (cosine rule)
						var b = pos - dockPos.Value;

						if (b != WVec.Zero)
						{
							var c = pos - harvPos;
							if (c != WVec.Zero)
							{
								var a = harvPos - dockPos.Value;
								var cosA = (int)(512 * (b.LengthSquared + c.LengthSquared - a.LengthSquared) / b.Length / c.Length);

								// Cost modifier varies between 0 and ResourceRefineryDirectionPenalty
								return Math.Abs(harvInfo.ResourceRefineryDirectionPenalty / 2) + harvInfo.ResourceRefineryDirectionPenalty * cosA / 2048;
							}
						}
					}

					return 0;
				});

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

			if (orderLocation != null)
				yield return new TargetLineNode(Target.FromCell(self.World, orderLocation.Value), harvInfo.HarvestLineColor);
			else
			{
				var manager = harv.DockClientManager;
				if (manager.ReservedHostActor != null)
					yield return new TargetLineNode(Target.FromActor(manager.ReservedHostActor), manager.DockLineColor);
			}
		}
	}
}
