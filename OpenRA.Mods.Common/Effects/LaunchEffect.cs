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

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Effects
{
	public class LaunchEffect : IEffect, ISpatiallyPartitionable
	{
		readonly World world;
		readonly Animation anim;
		readonly string palette;
		WPos pos;
		Func<WPos> posFunc;

		public LaunchEffect(World world, string image, string sequence, string palette)
			: this(world, () => WPos.Zero, () => 0, image, sequence, palette) { }

		public LaunchEffect(World world, Func<WPos> posFunc, Func<int> facingFunc, string image, string sequence, string palette)
		{
			this.world = world;
			this.posFunc = posFunc;
			this.palette = palette;

			anim = new Animation(world, image, facingFunc);
			anim.PlayThen(sequence, () => world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); }));
			pos = posFunc();
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
			if (world.FogObscures(pos))
				return SpriteRenderable.None;

			return anim.Render(pos, wr.Palette(palette));
		}
	}
}
