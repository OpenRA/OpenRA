#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FindResources : Activity
	{
		CPos? avoidCell;

		public FindResources()
		{
		}

		public FindResources(CPos avoidCell)
		{
			this.avoidCell = avoidCell;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || NextActivity != null) return NextActivity;

			var harv = self.Trait<Harvester>();

			if (harv.IsFull)
				return Util.SequenceActivities(new DeliverResources(), NextActivity);

			var harvInfo = self.Info.Traits.Get<HarvesterInfo>();
			var mobile = self.Trait<Mobile>();
			var mobileInfo = self.Info.Traits.Get<MobileInfo>();
			var resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			var territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();

			// Determine where to search from and how far to search:
			var searchFromLoc = harv.LastOrderLocation ?? (harv.LastLinkedProc ?? harv.LinkedProc ?? self).Location;
			var searchRadius = harv.LastOrderLocation.HasValue ? harvInfo.SearchFromOrderRadius : harvInfo.SearchFromProcRadius;
			var searchRadiusSquared = searchRadius * searchRadius;

			// Pick the available harvestable positions inside the harvestable radius from the selected point.
			var harvestablePositions = self.World.Map.FindTilesInCircle(searchFromLoc, searchRadius)
				.Where(loc =>
				{
					var resType = resLayer.GetResource(loc);
					if (resType == null)
						return false;

					// Can the harvester collect this kind of resource?
					if (!harvInfo.Resources.Contains(resType.Info.Name))
						return false;

					if (territory != null)
					{
						// Another harvester has claimed this resource:
						ResourceClaim claim;
						if (territory.IsClaimedByAnyoneElse(self, loc, out claim))
							return false;
					}

					return true;
				}).ToList();

			List<CPos> path;

			// If no harvestable position could be found, then return an empty path
			if (harvestablePositions.Count == 0)
				path = new List<CPos>();
			else
			{
				// Pick the closest harvestable position from the actor
				var closestHarvestablePosition = harvestablePositions.MinBy(loc => (loc - self.Location).Length);

				// Find a path to that harvestable resource
				path = self.World.WorldActor.Trait<IPathFinder>().FindPath(
					PathSearch.FromPoint(self.World, mobileInfo, self, self.Location, closestHarvestablePosition, true)
						.WithCustomCost(loc =>
						{
							// Avoid this cell
							if (avoidCell.HasValue && loc == avoidCell.Value) return Constants.InvalidNode;

							// Don't harvest out of range:
							var distSquared = (loc - searchFromLoc).LengthSquared;
							if (distSquared > searchRadiusSquared) return Constants.InvalidNode;

							return 0;
						}));
			}

			if (path.Count == 0)
			{
				if (!harv.IsEmpty)
					return new DeliverResources();
				else
				{
					// Get out of the way if we are:
					harv.UnblockRefinery(self);
					var randFrames = 125 + self.World.SharedRandom.Next(-35, 35);
					if (NextActivity != null)
						return Util.SequenceActivities(NextActivity, new Wait(randFrames), new FindResources());
					else
						return Util.SequenceActivities(new Wait(randFrames), new FindResources());
				}
			}

			// Attempt to claim a resource as ours:
			if (territory != null)
			{
				if (!territory.ClaimResource(self, path[0]))
					return Util.SequenceActivities(new Wait(25), new FindResources());
			}

			// If not given a direct order, assume ordered to the first resource location we find:
			if (harv.LastOrderLocation == null)
				harv.LastOrderLocation = path[0];

			self.SetTargetLine(Target.FromCell(self.World, path[0]), Color.Red, false);

			var notify = self.TraitsImplementing<INotifyHarvesterAction>();
			var next = new FindResources();
			foreach (var n in notify)
				n.MovingToResources(self, path[0], next);

			return Util.SequenceActivities(mobile.MoveTo(path[0], 1), new HarvestResource(), new FindResources());
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromCell(self.World, self.Location);
		}
	}

	public class HarvestResource : Activity
	{
		public override Activity Tick(Actor self)
		{
			var territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
			if (IsCanceled)
			{
				if (territory != null)
					territory.UnclaimByActor(self);
				return NextActivity;
			}

			var harv = self.Trait<Harvester>();
			var harvInfo = self.Info.Traits.Get<HarvesterInfo>();
			harv.LastHarvestedCell = self.Location;

			if (harv.IsFull)
			{
				if (territory != null)
					territory.UnclaimByActor(self);
				return NextActivity;
			}

			// Turn to one of the harvestable facings
			if (harvInfo.HarvestFacings != 0)
			{
				var facing = self.Trait<IFacing>().Facing;
				var desired = Util.QuantizeFacing(facing, harvInfo.HarvestFacings) * (256 / harvInfo.HarvestFacings);
				if (desired != facing)
					return Util.SequenceActivities(new Turn(self, desired), this);
			}

			var resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			var resource = resLayer.Harvest(self.Location);
			if (resource == null)
			{
				if (territory != null)
					territory.UnclaimByActor(self);
				return NextActivity;
			}

			harv.AcceptResource(resource);

			foreach (var t in self.TraitsImplementing<INotifyHarvesterAction>())
				t.Harvested(self, resource);

			return Util.SequenceActivities(new Wait(harvInfo.LoadTicksPerBale), this);
		}
	}
}
