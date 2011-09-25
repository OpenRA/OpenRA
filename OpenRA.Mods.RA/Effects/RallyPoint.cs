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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
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

		int2 cachedLocation;
		public void Tick( World world )
		{
			flag.Tick();
			circles.Tick();
			if (cachedLocation != rp.rallyPoint)
			{
				cachedLocation = rp.rallyPoint;
				circles.Play("circles");
			}

			if (!building.IsInWorld || building.IsDead())
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			if (building.IsInWorld && building.Owner == building.World.LocalPlayer
				&& building.World.Selection.Actors.Contains(building))
			{
				var pos = Traits.Util.CenterOfCell(rp.rallyPoint);
				var palette = building.Trait<RenderSimple>().Palette(building.Owner);

				yield return new Renderable(circles.Image,
					pos - .5f * circles.Image.size,
					palette, (int)pos.Y);

				yield return new Renderable(flag.Image,
					pos + new float2(-1,-17),
					palette, (int)pos.Y);
			}
		}
	}
}
