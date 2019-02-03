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

		CPos? avoidCell;

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

		public FindResources(Actor self, CPos avoidCell)
			: this(self)
		{
			this.avoidCell = avoidCell;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if (harv.IsFull)
			{
				// HACK: DeliverResources is ignored if there are queued activities, so discard NextActivity
				return ActivityUtils.SequenceActivities(new DeliverResources(self));
			}

			var closestHarvestablePosition = ClosestHarvestablePos(self);

			// If no harvestable position could be found, either deliver the remaining resources
			// or get out of the way and do not disturb.
			if (!closestHarvestablePosition.HasValue)
			{
				if (!harv.IsEmpty)
					return new DeliverResources(self);

				harv.LastSearchFailed = true;

				var unblockCell = harv.LastHarvestedCell ?? (self.Location + harvInfo.UnblockCell);
				var moveTo = mobile.NearestMoveableCell(unblockCell, 2, 5);
				self.QueueActivity(mobile.MoveTo(moveTo, 1));

				foreach (var n in self.TraitsImplementing<INotifyHarvesterAction>())
					n.MovingToResources(self, moveTo, this);

				self.SetTargetLine(Target.FromCell(self.World, moveTo), Color.Gray, false);
				var randFrames = self.World.SharedRandom.Next(100, 175);

				// Avoid creating an activity cycle
				var next = NextInQueue;
				NextInQueue = null;
				return ActivityUtils.SequenceActivities(next, new Wait(randFrames), this);
			}
			else
			{
				// Attempt to claim the target cell
				if (!claimLayer.TryClaimCell(self, closestHarvestablePosition.Value))
					return ActivityUtils.SequenceActivities(new Wait(25), this);

				harv.LastSearchFailed = false;

				// If not given a direct order, assume ordered to the first resource location we find:
				if (!harv.LastOrderLocation.HasValue)
					harv.LastOrderLocation = closestHarvestablePosition;

				foreach (var n in self.TraitsImplementing<INotifyHarvesterAction>())
					n.MovingToResources(self, closestHarvestablePosition.Value, this);

				self.SetTargetLine(Target.FromCell(self.World, closestHarvestablePosition.Value), Color.Red, false);
				return ActivityUtils.SequenceActivities(mobile.MoveTo(closestHarvestablePosition.Value, 1), new HarvestResource(self), this);
			}
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
			var searchFromLoc = GetSearchFromLocation(self);
			var searchRadius = harv.LastOrderLocation.HasValue ? harvInfo.SearchFromOrderRadius : harvInfo.SearchFromProcRadius;
			var searchRadiusSquared = searchRadius * searchRadius;

			// Find any harvestable resources:
			List<CPos> path;
			using (var search = PathSearch.Search(self.World, locomotorInfo, self, true, loc =>
					domainIndex.IsPassable(self.Location, loc, locomotorInfo) && harv.CanHarvestCell(self, loc) && claimLayer.CanClaimCell(self, loc))
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

		CPos GetSearchFromLocation(Actor self)
		{
			if (harv.LastOrderLocation.HasValue)
				return harv.LastOrderLocation.Value;
			else if (harv.LastLinkedProc != null)
				return harv.LastLinkedProc.Location + harv.LastLinkedProc.Trait<IAcceptResources>().DeliveryOffset;
			else if (harv.LinkedProc != null)
				return harv.LinkedProc.Location + harv.LinkedProc.Trait<IAcceptResources>().DeliveryOffset;
			return self.Location;
		}
	}
}
