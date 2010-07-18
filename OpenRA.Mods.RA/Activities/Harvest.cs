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
	public class Harvest : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isHarvesting = false;

		public IActivity Tick( Actor self )
		{
			if( isHarvesting ) return this;
			if( NextActivity != null ) return NextActivity;

			var harv = self.traits.Get<Harvester>();
			harv.LastHarvestedCell = self.Location;

			if( harv.IsFull )
				return new DeliverResources { NextActivity = NextActivity };

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
			var harv = self.traits.Get<Harvester>();
			var renderUnit = self.traits.Get<RenderUnit>();	/* better have one of these! */

			var resource = self.World.WorldActor.traits.Get<ResourceLayer>().Harvest(self.Location);
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
			var res = self.World.WorldActor.traits.Get<ResourceLayer>();
			var harv = self.Info.Traits.Get<HarvesterInfo>();

			self.QueueActivity(new Move(
				() =>
				{
					return self.World.PathFinder.FindPath(PathSearch.Search(self, true)
						.WithHeuristic(loc => (res.GetResource(loc) != null && harv.Resources.Contains( res.GetResource(loc).info.Name )) ? 0 : 1)
				        .FromPoint(self.Location));
				}));
			self.QueueActivity(new Harvest());
		}

		public void Cancel(Actor self) { }
	}
}
