#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.AS.Effects
{
	class SmokeParticle : IEffect
	{
		readonly World world;
		readonly string palette;
		readonly Animation anim;
		readonly WVec[] gravity;
		readonly bool visibleThroughFog;
		readonly bool scaleSizeWithZoom;

		WPos pos;

		public SmokeParticle(WPos pos, WVec[] gravity, World world, string image, string sequence, string palette, bool visibleThroughFog = false, bool scaleSizeWithZoom = false)
		{
			this.world = world;
			this.pos = pos;
			this.gravity = gravity;
			this.palette = palette;
			this.scaleSizeWithZoom = scaleSizeWithZoom;
			this.visibleThroughFog = visibleThroughFog;
			anim = new Animation(world, image, () => 0);
			anim.PlayThen(sequence, () => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick(World world)
		{
			anim.Tick();

			var offset = gravity.Length == 2
				? new WVec(world.SharedRandom.Next(gravity[0].X, gravity[1].X), world.SharedRandom.Next(gravity[0].Y, gravity[1].Y),
					world.SharedRandom.Next(gravity[0].Z, gravity[1].Z))
				: gravity[0];

			pos += offset;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (world.FogObscures(pos) && !visibleThroughFog)
				return SpriteRenderable.None;

			var zoom = scaleSizeWithZoom ? 1f / wr.Viewport.Zoom : 1f;
			return anim.Render(pos, WVec.Zero, 0, wr.Palette(palette), zoom);
		}
	}
}
