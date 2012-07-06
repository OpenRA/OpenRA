#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using System;

namespace OpenRA.Mods.RA.Activities
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
			int searchRadius = harv.LastOrderLocation.HasValue ? harvInfo.SearchFromOrderRadius : harvInfo.SearchFromProcRadius;
			int searchRadiusSquared = searchRadius * searchRadius;

			// Find harvestable resources nearby:
			var path = self.World.WorldActor.Trait<PathFinder>().FindPath(
				PathSearch.Search(self.World, mobileInfo, self, true)
					.WithCustomCost(loc =>
					{
						// Avoid enemy territory:
						int safetycost = (
							// TODO: calculate weapons ranges of units and factor those in instead of hard-coding 8.
							from u in self.World.FindUnitsInCircle(loc.ToPPos(), Game.CellSize * 8)
							where !u.Destroyed
							where self.Owner.Stances[u.Owner] == Stance.Enemy
							select Math.Max(0, 64 - (loc - u.Location).LengthSquared)
						).Sum();

						return safetycost;
					})
					.WithHeuristic(loc =>
					{
						// Avoid this cell:
						if (avoidCell.HasValue && loc == avoidCell.Value) return 1;

						// Don't harvest out of range:
						int distSquared = (loc - searchFromLoc).LengthSquared;
						if (distSquared > searchRadiusSquared)
							return int.MaxValue;

						// Get the resource at this location:
						var resType = resLayer.GetResource(loc);

						if (resType == null) return 1;
						// Can the harvester collect this kind of resource?
						if (!harvInfo.Resources.Contains(resType.info.Name)) return 1;

						if (territory != null)
						{
							// Another harvester has claimed this resource:
							ResourceClaim claim;
							if (territory.IsClaimedByAnyoneElse(self, loc, out claim)) return 1;
						}

						return 0;
					})
					.FromPoint(self.Location)
			);

			if (path.Count == 0)
			{
				if (!harv.IsEmpty)
					return new DeliverResources();
				else
				{
					// Get out of the way if we are:
					harv.UnblockRefinery(self);
					int randFrames = 125 + self.World.SharedRandom.Next(-35, 35);
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

			self.SetTargetLine(Target.FromCell(path[0]), Color.Red, false);
			return Util.SequenceActivities(mobile.MoveTo(path[0], 1), new HarvestResource(), new FindResources());
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromCell(self.Location);
		}
	}

	public class HarvestResource : Activity
	{
		bool isHarvesting = false;

		public override Activity Tick(Actor self)
		{
			if (isHarvesting) return this;

			var territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
			if (IsCanceled)
			{
				if (territory != null) territory.UnclaimByActor(self);
				return NextActivity;
			}

			var harv = self.Trait<Harvester>();
			harv.LastHarvestedCell = self.Location;

			if (harv.IsFull)
			{
				if (territory != null) territory.UnclaimByActor(self);
				return NextActivity;
			}

			var resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			var resource = resLayer.Harvest(self.Location);
			if (resource == null)
			{
				if (territory != null) territory.UnclaimByActor(self);
				return NextActivity;
			}

			var renderUnit = self.Trait<RenderUnit>();	/* better have one of these! */
			if (renderUnit.anim.CurrentSequence.Name != "harvest")
			{
				isHarvesting = true;
				renderUnit.PlayCustomAnimation(self, "harvest", () => isHarvesting = false);
			}

			harv.AcceptResource(resource);
			return this;
		}
	}
}
