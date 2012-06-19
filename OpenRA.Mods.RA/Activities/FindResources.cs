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

namespace OpenRA.Mods.RA.Activities
{
	public class FindResources : Activity
	{
		public override Activity Tick(Actor self)
		{
			if (IsCanceled || NextActivity != null) return NextActivity;

			var harv = self.Trait<Harvester>();

			// TODO(jsd): Want to add the condition that if no more resources are available nearby, drop off
			// the current load, regardless of whether or not it is full. It's better to have resources collected
			// in a processor than sitting idle in a harvester.
			if (harv.IsFull)
				return Util.SequenceActivities(new DeliverResources(), NextActivity);

			var harvInfo = self.Info.Traits.Get<HarvesterInfo>();
			var mobile = self.Trait<Mobile>();
			var mobileInfo = self.Info.Traits.Get<MobileInfo>();
			var resLayer = self.World.WorldActor.Trait<ResourceLayer>();

			// Find harvestable resources nearby:
			var path = self.World.WorldActor.Trait<PathFinder>().FindPath(
				PathSearch.Search(self.World, mobileInfo, self.Owner, true)
						  .WithHeuristic(loc =>
							{
								// Get the resource at this location:
								var resType = resLayer.GetResource(loc);

								if (resType == null) return 1;
								// Can the harvester collect this kind of resource?
								if (!harvInfo.Resources.Contains(resType.info.Name)) return 1;
								// Another harvester has claimed this resource:
								if (resLayer.IsClaimedByAnyoneElse(self, loc)) return 1;

								// TODO(jsd): Add preferences to PathSearch. Want to prefer more dense resources over less dense resources,
								// but this code forcibly ignores less dense resources.
								//// Avoid less dense resources:
								//if (resLayer.GetResourceDensity(loc) < 1) return 1;

								return 0;
							})
				// Start searching either from the last harvested cell or our current position:
						  .FromPoint(harv.LastHarvestedCell ?? self.Location)
			);

			if (path.Count == 0)
				return NextActivity;

			// NOTE(jsd): do not iterate through path[n] because the current location
			// may be included in the list and moving to the current location will
			// cause the activity code to go into an infinite loop.

			// Attempt to claim a resource as ours:
			if (!resLayer.ClaimResource(self, path[0]))
				return this;

			self.SetTargetLine(Target.FromCell(path[0]), Color.Red, false);
			return Util.SequenceActivities(mobile.MoveTo(path[0], 1), new HarvestResource(), this);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(self.Location);
		}
	}

	public class HarvestResource : Activity
	{
		bool isHarvesting = false;

		public override Activity Tick(Actor self)
		{
			if (isHarvesting) return this;

			var resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			if (IsCanceled)
			{
				resLayer.UnclaimAllResourcesBy(self);
				return NextActivity;
			}

			var harv = self.Trait<Harvester>();
			harv.LastHarvestedCell = self.Location;

			if (harv.IsFull)
			{
				resLayer.UnclaimAllResourcesBy(self);
				return NextActivity;
			}

			var renderUnit = self.Trait<RenderUnit>();	/* better have one of these! */
			var resource = resLayer.Harvest(self.Location);
			if (resource == null)
			{
				resLayer.UnclaimAllResourcesBy(self);
				return NextActivity;
			}

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
