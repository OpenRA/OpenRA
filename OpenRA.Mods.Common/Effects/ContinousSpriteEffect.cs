#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class ContinousSpriteEffect : IEffect, ISpatiallyPartitionable
	{
		readonly World world;
		readonly string palette;
		readonly Animation anim;
		readonly bool visibleThroughFog;
		readonly string sequence;
		readonly WPos pos;
		bool initialized;

		public ContinousSpriteEffect(WPos pos, World world, string image, string sequence, string palette, bool visibleThroughFog)
		{
			this.pos = pos;
			this.world = world;
			this.palette = palette;
			this.sequence = sequence;
			this.visibleThroughFog = visibleThroughFog;
			anim = new Animation(world, image);
		}

		public void Tick(World world)
		{
			if (!initialized)
			{
				anim.PlayRepeating(sequence);
				world.ScreenMap.Add(this, pos, anim.Image);
				initialized = true;
			}
			else
				anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!initialized || (!visibleThroughFog && world.FogObscures(pos)))
				return SpriteRenderable.None;

			return anim.Render(pos, wr.Palette(palette));
		}
	}
}
