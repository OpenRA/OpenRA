#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
		readonly string sequence;
		WPos pos;
		int delay;
		bool initialized;

		// Facing is last on these overloads partially for backwards compatibility with previous main ctor revision
		// and partially because most effects don't need it. The latter is also the reason for placement of 'delay'.
		public SpriteEffect(WPos pos, World world, string image, string sequence, string palette,
			bool visibleThroughFog = false, int delay = 0)
			: this(() => pos, () => WAngle.Zero, world, image, sequence, palette, visibleThroughFog, delay) { }

		public SpriteEffect(Actor actor, World world, string image, string sequence, string palette,
			bool visibleThroughFog = false, int delay = 0)
			: this(() => actor.CenterPosition, () => WAngle.Zero, world, image, sequence, palette, visibleThroughFog, delay) { }

		public SpriteEffect(WPos pos, WAngle facing, World world, string image, string sequence, string palette,
			bool visibleThroughFog = false, int delay = 0)
			: this(() => pos, () => facing, world, image, sequence, palette, visibleThroughFog, delay) { }

		public SpriteEffect(Func<WPos> posFunc, Func<WAngle> facingFunc, World world, string image, string sequence, string palette,
			bool visibleThroughFog = false, int delay = 0)
		{
			this.world = world;
			this.posFunc = posFunc;
			this.palette = palette;
			this.sequence = sequence;
			this.visibleThroughFog = visibleThroughFog;
			this.delay = delay;
			pos = posFunc();
			anim = new Animation(world, image, facingFunc);
		}

		public void Tick(World world)
		{
			if (delay-- > 0)
				return;

			if (!initialized)
			{
				anim.PlayThen(sequence, () => world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); }));
				world.ScreenMap.Add(this, pos, anim.Image);
				initialized = true;
			}
			else
			{
				anim.Tick();

				pos = posFunc();
				world.ScreenMap.Update(this, pos, anim.Image);
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!initialized || (!visibleThroughFog && world.FogObscures(pos)))
				return SpriteRenderable.None;

			return anim.Render(pos, wr.Palette(palette));
		}
	}
}
