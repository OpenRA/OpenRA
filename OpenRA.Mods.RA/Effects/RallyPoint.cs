#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class RallyPoint : IEffect
	{
		Actor building;
		RA.RallyPoint rp;
		public Animation flag = new Animation("rallypoint");
		public Animation circles = new Animation("rallypoint");
		
		public RallyPoint(Actor building) 
		{ 
			this.building = building;
			rp = building.Trait<RA.RallyPoint>();
			flag.PlayRepeating("flag");
			circles.Play("circles");
		}

		public void Tick( World world )
		{
			flag.Tick();
			circles.Tick();
			if (!building.IsInWorld || building.IsDead())
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			if (building.IsInWorld && building.Owner == building.World.LocalPlayer && building.World.Selection.Actors.Contains(building))
			{
				var pos = Traits.Util.CenterOfCell(rp.rallyPoint);
				yield return new Renderable(circles.Image, 
					pos - .5f * circles.Image.size, 
					building.Owner.Palette, (int)pos.Y);

				yield return new Renderable(flag.Image, 
					pos + new float2(-1,-17), 
					building.Owner.Palette, (int)pos.Y);
			}
		}
	}
}
