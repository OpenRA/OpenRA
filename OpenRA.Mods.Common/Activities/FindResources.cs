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
		readonly Harvester harv;
		readonly HarvesterInfo harvInfo;
		readonly Mobile mobile;
		readonly MobileInfo mobileInfo;
		readonly ResourceLayer resLayer;
		readonly ResourceClaimLayer territory;
		readonly IPathFinder pathFinder;

		CPos? avoidCell;

		public FindResources(Actor self)
		{
			harv = self.Trait<Harvester>();
			harvInfo = self.Info.Traits.Get<HarvesterInfo>();
			mobile = self.Trait<Mobile>();
			mobileInfo = self.Info.Traits.Get<MobileInfo>();
			resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
			pathFinder = self.World.WorldActor.Trait<IPathFinder>();
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
				return Util.SequenceActivities(deliver, NextActivity);

			// Determine where to search from and how far to search:
			var searchFromLoc = harv.LastOrderLocation ?? (harv.LastLinkedProc ?? harv.LinkedProc ?? self).Location;
			var searchRadius = harv.LastOrderLocation.HasValue ? harvInfo.SearchFromOrderRadius : harvInfo.SearchFromProcRadius;
			var searchRadiusSquared = searchRadius * searchRadius;

			// Find harvestable resources nearby:
			var path = pathFinder.FindPath(
				PathSearch.Search(self.World, mobileInfo, self, true)
					.WithHeuristic(loc =>
					{
						// Avoid this cell:
						if (avoidCell.HasValue && loc == avoidCell.Value)
							return EstimateDistance(loc, searchFromLoc) + Constants.CellCost;

						// Don't harvest out of range:
						var distSquared = (loc - searchFromLoc).LengthSquared;
						if (distSquared > searchRadiusSquared)
							return EstimateDistance(loc, searchFromLoc) + Constants.CellCost * 2;

						// Get the resource at this location:
						var resType = resLayer.GetResource(loc);
						if (resType == null)
							return EstimateDistance(loc, searchFromLoc) + Constants.CellCost;

						// Can the harvester collect this kind of resource?
						if (!harvInfo.Resources.Contains(resType.Info.Name))
							return EstimateDistance(loc, searchFromLoc) + Constants.CellCost;

						if (territory != null)
						{
							// Another harvester has claimed this resource:
							ResourceClaim claim;
							if (territory.IsClaimedByAnyoneElse(self, loc, out claim))
								return EstimateDistance(loc, searchFromLoc) + Constants.CellCost;
						}

						return 0;
					})
					.FromPoint(self.Location));

			var next = this;

			if (path.Count == 0)
			{
				if (!harv.IsEmpty)
					return deliver;
				else
				{
					// Get out of the way if we are:
					harv.UnblockRefinery(self);
					var randFrames = self.World.SharedRandom.Next(90, 160);
					if (NextActivity != null)
						return Util.SequenceActivities(NextActivity, new Wait(randFrames), next);
					else
						return Util.SequenceActivities(new Wait(randFrames), next);
				}
			}

			// Attempt to claim a resource as ours:
			if (territory != null)
			{
				if (!territory.ClaimResource(self, path[0]))
					return Util.SequenceActivities(new Wait(25), next);
			}

			// If not given a direct order, assume ordered to the first resource location we find:
			if (harv.LastOrderLocation == null)
				harv.LastOrderLocation = path[0];

			self.SetTargetLine(Target.FromCell(self.World, path[0]), Color.Red, false);

			var notify = self.TraitsImplementing<INotifyHarvesterAction>();
			foreach (var n in notify)
				n.MovingToResources(self, path[0], next);

			return Util.SequenceActivities(mobile.MoveTo(path[0], 1), new HarvestResource(self), next);
		}

		// Diagonal distance heuristic
		static int EstimateDistance(CPos here, CPos destination)
		{
			var diag = Math.Min(Math.Abs(here.X - destination.X), Math.Abs(here.Y - destination.Y));
			var straight = Math.Abs(here.X - destination.X) + Math.Abs(here.Y - destination.Y);

			return Constants.CellCost * straight + (Constants.DiagonalCellCost - 2 * Constants.CellCost) * diag;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromCell(self.World, self.Location);
		}
	}
}
