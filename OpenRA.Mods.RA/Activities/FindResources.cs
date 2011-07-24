#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Activities
{
	public class FindResources : Activity
	{
		public override Activity Tick( Actor self )
		{
			if( IsCanceled || NextActivity != null) return NextActivity;

			var harv = self.Trait<Harvester>();
			if( harv.IsFull )
				return Util.SequenceActivities( new DeliverResources(), NextActivity );

			var mobileInfo = self.Info.Traits.Get<MobileInfo>();
			var harvInfo = self.Info.Traits.Get<HarvesterInfo>();
			var res = self.World.WorldActor.Trait<ResourceLayer>();

			Func<int2, bool> canHarvest = loc => loc != self.Location &&
				res.GetResource(loc) != null &&
				harvInfo.Resources.Contains( res.GetResource(loc).info.Name );

			var path = self.World.WorldActor.Trait<PathFinder>().FindPath(PathSearch.Search(self.World, mobileInfo, self.Owner, true)
						.WithHeuristic(loc => canHarvest(loc) ? 0 : 1)
				        .FromPoint(self.Location));

			if (path.Count == 0)
				return NextActivity;

			self.SetTargetLine(Target.FromCell(path[0]), Color.Red, false);
			return Util.SequenceActivities( new MoveAdjacentTo(Target.FromCell(path[0])), new HarvestResource(self, path[0]), this );
		}

		public override IEnumerable<Target> GetTargets( Actor self )
		{
			yield return Target.FromPos(self.Location);
		}
	}
}
