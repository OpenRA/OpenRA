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
		readonly Actor building;
		readonly RA.RallyPoint rp;
		readonly string palettePrefix;
		public Animation flag = new Animation("rallypoint");
		public Animation circles = new Animation("rallypoint");

		public RallyPoint(Actor building, string palettePrefix)
		{
			this.building = building;
			rp = building.Trait<RA.RallyPoint>();
			this.palettePrefix = palettePrefix;
			flag.PlayRepeating("flag");
			circles.Play("circles");
		}

		CPos cachedLocation;
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

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (building.IsInWorld && building.Owner == building.World.LocalPlayer
				&& building.World.Selection.Actors.Contains(building))
			{
				var pos = Traits.Util.CenterOfCell(rp.rallyPoint);
				var palette = wr.Palette(palettePrefix+building.Owner.InternalName);

				yield return new SpriteRenderable(circles.Image,
					pos.ToFloat2() - .5f * circles.Image.size,
					palette, (int)pos.Y);

				yield return new SpriteRenderable(flag.Image,
					pos.ToFloat2() + new float2(-1,-17),
					palette, (int)pos.Y);
			}
		}
	}
}
