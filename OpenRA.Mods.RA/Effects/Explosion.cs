#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA.Effects
{
	public class Explosion : IEffect
	{
		World world;
		WPos pos;
		CPos cell;
		string palette;
		Animation anim;

		public Explosion(World world, WPos pos, string sequence, string palette)
		{
			this.world = world;
			this.pos = pos;
			this.cell = world.Map.CellContaining(pos);
			this.palette = palette;
			anim = new Animation(world, "explosion");
			anim.PlayThen(sequence, () => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick(World world) { anim.Tick(); }

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (world.FogObscures(cell))
				return SpriteRenderable.None;

			return anim.Render(pos, wr.Palette(palette));
		}
	}
}
