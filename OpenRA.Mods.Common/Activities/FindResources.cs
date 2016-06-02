#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FindResources : Activity
	{
		readonly Harvester harv;
		readonly HarvesterInfo harvInfo;
		readonly Mobile mobile;
		readonly MobileInfo mobileInfo;
		readonly ResourceLayer resLayer;
		readonly ResourceClaimLayer territory;
		readonly IPathFinder pathFinder;
		readonly DomainIndex domainIndex;

		CPos? avoidCell;

		public FindResources(Actor self)
		{
			harv = self.Trait<Harvester>();
			harvInfo = self.Info.TraitInfo<HarvesterInfo>();
			mobile = self.Trait<Mobile>();
			mobileInfo = self.Info.TraitInfo<MobileInfo>();
			resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
			pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();
		}

		public FindResources(Actor self, CPos avoidCell)
			: this(self)
		{
			this.avoidCell = avoidCell;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || NextActivity != null)
				return NextActivity;

			var deliver = new DeliverResources(self);

			if (harv.IsFull)
				return ActivityUtils.SequenceActivities(deliver, NextActivity);

			var closestHarvestablePosition = ClosestHarvestablePos(self);

			// If no harvestable position could be found, either deliver the remaining resources
			// or get out of the way and do not disturb.
			if (!closestHarvestablePosition.HasValue)
			{
				if (!harv.IsEmpty)
					return deliver;

				var unblockCell = harv.LastHarvestedCell ?? (self.Location + harvInfo.UnblockCell);
				var moveTo = mobile.NearestMoveableCell(unblockCell, 2, 5);
				self.QueueActivity(mobile.MoveTo(moveTo, 1));
				self.SetTargetLine(Target.FromCell(self.World, moveTo), Color.Gray, false);

				var randFrames = self.World.SharedRandom.Next(100, 175);
				return ActivityUtils.SequenceActivities(NextActivity, new Wait(randFrames), this);
			}
			else
			{
				var next = this;

				// Attempt to claim a resource as ours
				if (territory != null)
				{
					if (!territory.ClaimResource(self, closestHarvestablePosition.Value))
						return ActivityUtils.SequenceActivities(new Wait(25), next);
				}

				// If not given a direct order, assume ordered to the first resource location we find:
				if (!harv.LastOrderLocation.HasValue)
					harv.LastOrderLocation = closestHarvestablePosition;

				self.SetTargetLine(Target.FromCell(self.World, closestHarvestablePosition.Value), Color.Red, false);

				var notify = self.TraitsImplementing<INotifyHarvesterAction>();

				foreach (var n in notify)
					n.MovingToResources(self, closestHarvestablePosition.Value, next);

				return ActivityUtils.SequenceActivities(mobile.MoveTo(closestHarvestablePosition.Value, 1), new HarvestResource(self), next);
			}
		}

		/// <summary>
		/// Finds the closest harvestable pos between the current position of the harvester
		/// and the last order location
		/// </summary>
		CPos? ClosestHarvestablePos(Actor self)
		{
			if (self.CanHarvestAt(self.Location, resLayer, harvInfo, territory))
				return self.Location;

			// Determine where to search from and how far to search:
			var searchFromLoc = harv.LastOrderLocation ?? (harv.LastLinkedProc ?? harv.LinkedProc ?? self).Location;
			var searchRadius = harv.LastOrderLocation.HasValue ? harvInfo.SearchFromOrderRadius : harvInfo.SearchFromProcRadius;
			var searchRadiusSquared = searchRadius * searchRadius;

			// Find any harvestable resources:
			var passable = (uint)mobileInfo.GetMovementClass(self.World.Map.Rules.TileSet);
			List<CPos> path;
			using (var search = PathSearch.Search(self.World, mobileInfo, self, true,
				loc => domainIndex.IsPassable(self.Location, loc, passable) && self.CanHarvestAt(loc, resLayer, harvInfo, territory))
				.WithCustomCost(loc =>
				{
					if ((avoidCell.HasValue && loc == avoidCell.Value) ||
						(loc - self.Location).LengthSquared > searchRadiusSquared)
						return int.MaxValue;

					return 0;
				})
				.FromPoint(self.Location)
				.FromPoint(searchFromLoc))
				path = pathFinder.FindPath(search);

			if (path.Count > 0)
				return path[0];

			return null;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromCell(self.World, self.Location);
		}
	}
}
