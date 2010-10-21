#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class Harvest : CancelableActivity
	{
		bool isHarvesting = false;

		public override IActivity Tick( Actor self )
		{
			if( isHarvesting ) return this;
			if( IsCanceled ) return NextActivity;
			if( NextActivity != null ) return NextActivity;

			var harv = self.Trait<Harvester>();
			harv.LastHarvestedCell = self.Location;

			if( harv.IsFull )
				return Util.SequenceActivities( new DeliverResources(), NextActivity );

			if (HarvestThisTile(self))
				return this;
			else
			{
				FindMoreResource(self);
				return NextActivity;
			}
		}

		bool HarvestThisTile(Actor self)
		{
			var harv = self.Trait<Harvester>();
			var renderUnit = self.Trait<RenderUnit>();	/* better have one of these! */

			var resource = self.World.WorldActor.Trait<ResourceLayer>().Harvest(self.Location);
			if (resource == null)
				return false;
			
			if (renderUnit.anim.CurrentSequence.Name != "harvest")
			{
				isHarvesting = true;
				renderUnit.PlayCustomAnimation(self, "harvest", () => isHarvesting = false);
			}
			harv.AcceptResource(resource);
			return true;
		}

		void FindMoreResource(Actor self)
		{
			var mobile = self.Trait<Mobile>();
			var res = self.World.WorldActor.Trait<ResourceLayer>();
			var harv = self.Info.Traits.Get<HarvesterInfo>();
			var mobileInfo = self.Info.Traits.Get<MobileInfo>();
			self.QueueActivity(mobile.MoveTo(
				() =>
				{
					return self.World.PathFinder.FindPath(PathSearch.Search(self.World, mobileInfo, true)
						.WithHeuristic(loc => (res.GetResource(loc) != null && harv.Resources.Contains( res.GetResource(loc).info.Name )) ? 0 : 1)
				        .FromPoint(self.Location));
				}));
			self.QueueActivity(new Harvest());
		}
	}
}
