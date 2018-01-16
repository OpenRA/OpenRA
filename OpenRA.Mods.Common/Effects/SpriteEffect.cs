#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Effects
{
	public class SpriteEffect : IEffect, ISpatiallyPartitionable
	{
		readonly World world;
		readonly string palette;
		readonly Animation anim;
		readonly WPos pos;
		readonly bool visibleThroughFog;
		readonly bool scaleSizeWithZoom;

		public SpriteEffect(WPos pos, World world, string image, string sequence, string palette, bool visibleThroughFog = false, bool scaleSizeWithZoom = false, int facing = 0)
		{
			this.world = world;
			this.pos = pos;
			this.palette = palette;
			this.scaleSizeWithZoom = scaleSizeWithZoom;
			this.visibleThroughFog = visibleThroughFog;
			anim = new Animation(world, image, () => facing);
			anim.PlayThen(sequence, () => world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); }));
			world.ScreenMap.Add(this, pos, anim.Image);
		}

		public void Tick(World world)
		{
			anim.Tick();
			world.ScreenMap.Update(this, pos, anim.Image);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!visibleThroughFog && world.FogObscures(pos))
				return SpriteRenderable.None;

			var zoom = scaleSizeWithZoom ? 1f / wr.Viewport.Zoom : 1f;
			return anim.Render(pos, WVec.Zero, 0, wr.Palette(palette), zoom);
		}
	}
}
