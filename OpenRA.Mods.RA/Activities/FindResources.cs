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
			if (harv.IsFull)
				return Util.SequenceActivities(new DeliverResources(), NextActivity);

			var harvInfo = self.Info.Traits.Get<HarvesterInfo>();
			var mobile = self.Trait<Mobile>();
			var mobileInfo = self.Info.Traits.Get<MobileInfo>();
			var res = self.World.WorldActor.Trait<ResourceLayer>();
			var path = self.World.WorldActor.Trait<PathFinder>().FindPath(PathSearch.Search(self.World, mobileInfo, self.Owner, true)
						.WithHeuristic(loc => (res.GetResource(loc) != null && harvInfo.Resources.Contains(res.GetResource(loc).info.Name)) ? 0 : 1)
						.FromPoint(self.Location));

			if (path.Count == 0)
				return NextActivity;

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
			if (IsCanceled) return NextActivity;
			var harv = self.Trait<Harvester>();
			harv.LastHarvestedCell = self.Location;

			if (harv.IsFull)
				return NextActivity;

			var renderUnit = self.Trait<RenderUnit>();	/* better have one of these! */
			var resource = self.World.WorldActor.Trait<ResourceLayer>().Harvest(self.Location);
			if (resource == null)
				return NextActivity;

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
