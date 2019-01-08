#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
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
		readonly Func<WPos> posFunc;
		readonly bool visibleThroughFog;
		readonly bool scaleSizeWithZoom;
		WPos pos;

		// Facing is last on these overloads partially for backwards compatibility with previous main ctor revision
		// and partially because most effects don't need it.
		public SpriteEffect(WPos pos, World world, string image, string sequence, string palette, bool visibleThroughFog = false, bool scaleSizeWithZoom = false, int facing = 0)
			: this(() => pos, () => facing, world, image, sequence, palette, visibleThroughFog, scaleSizeWithZoom) { }

		public SpriteEffect(Actor actor, World world, string image, string sequence, string palette, bool visibleThroughFog = false, bool scaleSizeWithZoom = false, int facing = 0)
			: this(() => actor.CenterPosition, () => facing, world, image, sequence, palette, visibleThroughFog, scaleSizeWithZoom) { }

		public SpriteEffect(Func<WPos> posFunc, Func<int> facingFunc, World world, string image, string sequence, string palette,
			bool visibleThroughFog = false, bool scaleSizeWithZoom = false)
		{
			this.world = world;
			this.posFunc = posFunc;
			this.palette = palette;
			this.scaleSizeWithZoom = scaleSizeWithZoom;
			this.visibleThroughFog = visibleThroughFog;
			pos = posFunc();
			anim = new Animation(world, image, facingFunc);
			anim.PlayThen(sequence, () => world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); }));
			world.ScreenMap.Add(this, pos, anim.Image);
		}

		public void Tick(World world)
		{
			anim.Tick();

			pos = posFunc();
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
