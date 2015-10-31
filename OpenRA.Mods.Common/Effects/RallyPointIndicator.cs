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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Effects
{
	class RallyPointIndicator : IEffect
	{
		readonly Actor building;
		readonly RallyPoint rp;
		readonly Animation flag;
		readonly Animation circles;

		public RallyPointIndicator(Actor building, RallyPoint rp)
		{
			this.building = building;
			this.rp = rp;

			flag = new Animation(building.World, rp.Info.Image);
			flag.PlayRepeating(rp.Info.FlagSequence);

			circles = new Animation(building.World, rp.Info.Image);
			circles.Play(rp.Info.CirclesSequence);
		}

		CPos cachedLocation;
		public void Tick(World world)
		{
			flag.Tick();
			circles.Tick();

			if (cachedLocation != rp.Location)
			{
				cachedLocation = rp.Location;
				circles.Play(rp.Info.CirclesSequence);
			}

			if (!building.IsInWorld || building.IsDead)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (building.Owner != building.World.LocalPlayer)
				return SpriteRenderable.None;

			if (!building.IsInWorld || !building.World.Selection.Actors.Contains(building))
				return SpriteRenderable.None;

			var pos = wr.World.Map.CenterOfCell(cachedLocation);
			var palette = wr.Palette(rp.PaletteName);
			return circles.Render(pos, palette).Concat(flag.Render(pos, palette));
		}
	}
}
