#region Copyright & License Information
/*
 * CnP of FindResources.cs of OpenRA.
 * Modded by Boolbada of OP Mod
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.yupgi_alert.Traits;
using OpenRA.Traits;
using OpenRA.Mods.Common;

namespace OpenRA.Mods.yupgi_alert.Activities
{
	public class SpawnerHarvesterHarvest : Activity
	{	
		readonly SpawnerHarvester harv;
		readonly SpawnerHarvesterInfo harvInfo;
		readonly Mobile mobile;
		readonly MobileInfo mobileInfo;
		readonly ResourceLayer resLayer;
		readonly ResourceClaimLayer territory;
		readonly IPathFinder pathFinder;
		readonly DomainIndex domainIndex;
		readonly GrantConditionOnDeploy deploy;

		CPos? avoidCell;

		public SpawnerHarvesterHarvest(Actor self)
		{
			harv = self.Trait<SpawnerHarvester>();
			harvInfo = self.Info.TraitInfo<SpawnerHarvesterInfo>();
			mobile = self.Trait<Mobile>();
			mobileInfo = self.Info.TraitInfo<MobileInfo>();
			deploy = self.Trait<GrantConditionOnDeploy>();
			resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
			pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();
		}

		public SpawnerHarvesterHarvest(Actor self, CPos avoidCell)
			: this(self)
		{
			this.avoidCell = avoidCell;
		}

		Activity ScanTick(Actor self, out MiningState state)
		{
			state = MiningState.Scan;

			var searchFromLoc = harv.LastOrderLocation ?? self.Location;
			var closestHarvestablePosition = ClosestHarvestablePos(self, searchFromLoc, harvInfo.LongScanRadius);

			// If no harvestable position could be found, either deliver the remaining resources
			// or get out of the way and do not disturb.
			// Well... We don't need to deliver nor have to get out of the way, we are in the field.
			// We only have to wait for resource to regen.
			if (!closestHarvestablePosition.HasValue)
			{
				var randFrames = self.World.SharedRandom.Next(100, 175);
				// Avoid creating an activity cycle
				var next = NextInQueue;
				NextInQueue = null;
				return ActivityUtils.SequenceActivities(next, new Wait(randFrames), this);
			}

			// Attempt to claim a resource as ours
			// ... Don't claim yet. Slaves will claim by themselves.
			//if (territory != null && !territory.ClaimResource(self, closestHarvestablePosition.Value))
			//	return ActivityUtils.SequenceActivities(new Wait(25), this);

			// If not given a direct order, assume ordered to the first resource location we find:
			if (!harv.LastOrderLocation.HasValue)
				harv.LastOrderLocation = closestHarvestablePosition;

			self.SetTargetLine(Target.FromCell(self.World, closestHarvestablePosition.Value), Color.Red, false);

			// Calculate best depoly position.
			var deployPosition = calcDeployPosition(self, closestHarvestablePosition.Value);
			if (deployPosition == null)
				// Just sit there until we can. Won't happen unless the map is filled with units.
				return ActivityUtils.SequenceActivities(new Wait(harvInfo.KickDelay), this);

			// TODO: The harvest-deliver-return sequence is a horrible mess of duplicated code and edge-cases
			var notify = self.TraitsImplementing<INotifyHarvesterAction>();
			foreach (var n in notify)
				n.MovingToResources(self, deployPosition.Value, this);

			state = MiningState.CheckDeploy;
			return ActivityUtils.SequenceActivities(mobile.MoveTo(deployPosition.Value, 2), this);
		}

		Activity CheckDeployTick(Actor self, out MiningState state)
		{
			if (!deploy.CanDeployAtLocation(self.Location))
			{
				// If we can't deploy, go back to scan state so that we scan try deploy again.
				state = MiningState.Scan;
				return this;
			}

			// Issue deploy order and enter deploying state.
			state = MiningState.Deploying;
			if (deploy.DeployState == DeployState.Undeployed)
			{
				self.World.IssueOrder(new Order("GrantConditionOnDeploy", self, false));
				self.World.IssueOrder(new Order("SpawnerHarvestDeploying", self, true));
			}
			return null; // issuing order cancels everything so no use returning anything.
		}

		Activity DeployingTick(Actor self, out MiningState state)
		{
			state = MiningState.Deploying;
			if (deploy.DeployState == DeployState.Undeployed)
			{
				state = MiningState.CheckDeploy;
				return ActivityUtils.SequenceActivities(new Wait(15), this);
			}
			else if (deploy.DeployState == DeployState.Deploying)
			{
				return ActivityUtils.SequenceActivities(new Wait(15), this);
			}

			state = MiningState.Mining;
			return this;
		}

		Activity MiningTick(Actor self, out MiningState state)
		{
			state = MiningState.Kick;
			return ActivityUtils.SequenceActivities(new Wait(harvInfo.KickDelay), this);
		}

		Activity KickTick(Actor self, out MiningState state)
		{
			var closestHarvestablePosition = ClosestHarvestablePos(self, self.Location, harvInfo.KickScanRadius);
			if (closestHarvestablePosition.HasValue)
			{
				// I may stay mining.
				state = MiningState.Mining;
				return this;
			}

			// get going
			state = MiningState.Scan;
			self.World.IssueOrder(new Order("GrantConditionOnDeploy", self, false));
			return this;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if (NextInQueue != null)
				return NextInQueue;

			// Erm... looking at this, I could split these into separte activites...
			// I prefer finite state machine style though...
			// I can see what is going on at high level in this single place -_-
			// I think this is less horrible than OpenRA FindResources... stuff.
			// We are losing one tick, but so what?
			// If this loss isn't acceptable, call ATick() from BTick() or something.
			switch (harv.MiningState)
			{
				case MiningState.Scan:
					return ScanTick(self, out harv.MiningState);
				case MiningState.CheckDeploy:
					return CheckDeployTick(self, out harv.MiningState);
				case MiningState.Deploying:
					return DeployingTick(self, out harv.MiningState);
				case MiningState.Mining:
					return MiningTick(self, out harv.MiningState);
				case MiningState.Kick:
					return KickTick(self, out harv.MiningState);
				default:
					Game.Debug("SpawnHarvesterFindResources.cs in invalid state!");
					return null;
			}
		}

		// Find a nearest deployable position from harvestablePos
		CPos? calcDeployPosition(Actor self, CPos harvestablePos)
		{
			// FindTilesInAnnulus gives sorted cells by distance :) Nice.
			foreach (var tile in self.World.Map.FindTilesInAnnulus(harvestablePos, 0, harvInfo.DeployScanRadius))
				if (deploy.CanDeployAtLocation(tile))
					return tile;

			// Try broader search if unable to find deploy location
			foreach (var tile in self.World.Map.FindTilesInAnnulus(harvestablePos, harvInfo.DeployScanRadius, harvInfo.LongScanRadius))
				if (deploy.CanDeployAtLocation(tile))
					return tile;

			return null;
		}

		/// <summary>
		/// Finds the closest harvestable pos between the current position of the harvester
		/// and the last order location
		/// </summary>
		CPos? ClosestHarvestablePos(Actor self, CPos searchFromLoc, int radius)
		{
			if (self.CanHarvestAt(self.Location, resLayer, harvInfo, territory))
				return self.Location;

			// Determine where to search from and how far to search:
			var searchRadiusSquared = radius * radius;

			// Find any harvestable resources:
			var passable = (uint)mobileInfo.GetMovementClass(self.World.Map.Rules.TileSet);
			List<CPos> path;
			using (var search = PathSearch.Search(self.World, mobileInfo, self, true,
				loc => domainIndex.IsPassable(self.Location, loc, mobileInfo, passable) && self.CanHarvestAt(loc, resLayer, harvInfo, territory))
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
