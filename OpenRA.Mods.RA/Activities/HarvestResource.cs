#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA.Activities
{
	public class HarvestResource : Activity
	{
		Harvester harv;
		IFacing facing;
		RenderUnit renderUnit;
		ResourceLayer resourceLayer;
		bool isHarvesting = false;
		int2 harvestCell;

		public HarvestResource(Actor self, int2 cell)
		{
			harv = self.Trait<Harvester>();
			facing = self.Trait<IFacing>();
			renderUnit = self.Trait<RenderUnit>();
			resourceLayer = self.World.WorldActor.Trait<ResourceLayer>();
			harvestCell = cell;
		}

		public override Activity Tick( Actor self )
		{
			if( isHarvesting ) return this;
			if( IsCanceled ) return NextActivity;
			harv.LastHarvestedCell = harvestCell;

			if( harv.IsFull )
				return NextActivity;

			int2 dir = harvestCell - self.Location;
			var f = Util.GetFacing( dir, facing.Facing );
			if( f != facing.Facing )
				return Util.SequenceActivities( new Turn(f), this );

			var resource = resourceLayer.Harvest(harvestCell);
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
