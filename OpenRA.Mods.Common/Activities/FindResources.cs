#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
		ResourceLayer resLayer;
		HarvesterInfo harvInfo;
		ResourceClaimLayer territory;
		Mobile mobile;
		Harvester harv;
		IMobileInfo mobileInfo;
		IPathFinder pathFinder;

		// TODO: This is a hack because this class is sometimes
		// constructed by reflection and cannot add an Actor variable in
		// the constructor. Eventually remove!
		bool loadedVars = false;
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
			if (IsCanceled || NextActivity != null)
				return NextActivity;

			if (!loadedVars)
			{
				harvInfo = self.Info.Traits.Get<HarvesterInfo>();
				mobile = self.Trait<Mobile>();
				resLayer = self.World.WorldActor.Trait<ResourceLayer>();
				territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
				harv = self.Trait<Harvester>();
				mobileInfo = self.Info.Traits.Get<MobileInfo>();
				pathFinder = self.World.WorldActor.Trait<IPathFinder>();
				loadedVars = true;
			}

			if (harv.IsFull)
				return Util.SequenceActivities(new DeliverResources(), NextActivity);

			var closestHarvestablePosition = ClosestHarvestablePos(self);

			// If no harvestable position could be found, either deliver the remaining resources
			// or get out of the way and do not disturb.
			if (!closestHarvestablePosition.HasValue)
			{
				if (!harv.IsEmpty)
					return new DeliverResources();

				harv.UnblockRefinery(self);
				var moveTo = harv.LastHarvestedCell ?? (self.Location + new CVec(0, 4));
				self.QueueActivity(mobile.MoveTo(moveTo, 1));
				self.SetTargetLine(Target.FromCell(self.World, moveTo), Color.Gray, false);

				var randFrames = 125 + self.World.SharedRandom.Next(-35, 35);
				if (NextActivity != null)
					return Util.SequenceActivities(NextActivity, new Wait(randFrames), this);
				else
					return Util.SequenceActivities(new Wait(randFrames), this);
			}

			// Attempt to claim a resource as ours
			if (territory != null)
			{
				if (!territory.ClaimResource(self, closestHarvestablePosition.Value))
					return Util.SequenceActivities(new Wait(25), this);
			}

			// If not given a direct order, assume ordered to the first resource location we find:
			if (!harv.LastOrderLocation.HasValue)
				harv.LastOrderLocation = closestHarvestablePosition;

			self.SetTargetLine(Target.FromCell(self.World, closestHarvestablePosition.Value), Color.Red, false);

			var notify = self.TraitsImplementing<INotifyHarvesterAction>();
			var next = this;
			foreach (var n in notify)
				n.MovingToResources(self, closestHarvestablePosition.Value, next);

			return Util.SequenceActivities(mobile.MoveTo(closestHarvestablePosition.Value, 1), new HarvestResource(), next);
		}

		bool IsHarvestable(IActor self, CPos pos)
		{
			var resType = resLayer.GetResource(pos);
			if (resType == null)
				return false;

			// Can the harvester collect this kind of resource?
			if (!harvInfo.Resources.Contains(resType.Info.Name))
				return false;

			if (territory != null)
			{
				// Another harvester has claimed this resource:
				ResourceClaim claim;
				if (territory.IsClaimedByAnyoneElse(self as Actor, pos, out claim))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Finds the closest harvestable pos between the current position of the harvester
		/// and the last order location
		/// </summary>
		CPos? ClosestHarvestablePos(Actor self)
		{
			// Determine where to search from and how far to search:
			var searchFromLoc = harv.LastOrderLocation ?? (harv.LastLinkedProc ?? harv.LinkedProc ?? self).Location;
			var searchRadius = harv.LastOrderLocation.HasValue ? harvInfo.SearchFromOrderRadius : harvInfo.SearchFromProcRadius;
			var searchRadiusSquared = searchRadius * searchRadius;

			var search = PathSearch.Search(self.World, mobileInfo, self, true,
				loc => IsHarvestable(self, loc))
				.WithCustomCost(loc =>
				{
					if ((avoidCell.HasValue && loc == avoidCell.Value) ||
						(loc - self.Location).LengthSquared > searchRadiusSquared)
						return int.MaxValue;

					return 0;
				})
				.FromPoint(self.Location)
				.FromPoint(searchFromLoc);

			// Find any harvestable resources:
			var path = pathFinder.FindPath(search);

			if (path.Count > 0)
				return path[0];

			return null;
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
