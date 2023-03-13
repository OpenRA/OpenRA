#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	sealed class FloatingSprite : IEffect, ISpatiallyPartitionable
	{
		readonly WDist[] speed;
		readonly WDist[] gravity;
		readonly Animation anim;

		readonly bool visibleThroughFog;
		readonly int turnRate;
		readonly int randomRate;
		readonly string palette;

		WPos pos;
		WVec offset;
		int lifetime;
		int ticks;
		WAngle facing;

		public FloatingSprite(Actor emitter, string image, string[] sequences, string palette, bool isPlayerPalette,
			int[] lifetime, WDist[] speed, WDist[] gravity, int turnRate, int randomRate, WPos pos, WAngle facing,
			bool visibleThroughFog = false)
		{
			var world = emitter.World;
			this.pos = pos;
			this.turnRate = turnRate;
			this.randomRate = randomRate;
			this.speed = speed;
			this.gravity = gravity;
			this.visibleThroughFog = visibleThroughFog;
			this.facing = facing;

			anim = new Animation(world, image, () => facing);
			anim.PlayRepeating(sequences.Random(world.LocalRandom));
			world.ScreenMap.Add(this, pos, anim.Image);
			this.lifetime = Util.RandomInRange(world.LocalRandom, lifetime);

			this.palette = isPlayerPalette ? palette + emitter.Owner.InternalName : palette;
		}

		public void Tick(World world)
		{
			if (--lifetime < 0)
			{
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });
				return;
			}

			if (--ticks < 0)
			{
				var forward = Util.RandomDistance(world.LocalRandom, speed).Length;
				var height = Util.RandomDistance(world.LocalRandom, gravity).Length;

				offset = new WVec(forward, 0, height);

				if (turnRate > 0)
					facing = WAngle.FromFacing(Util.NormalizeFacing(facing.Facing + world.LocalRandom.Next(-turnRate, turnRate)));

				offset = offset.Rotate(WRot.FromYaw(facing));

				ticks = randomRate;
			}

			anim.Tick();

			pos += offset;

			world.ScreenMap.Update(this, pos, anim.Image);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!visibleThroughFog && wr.World.FogObscures(pos))
				return SpriteRenderable.None;

			return anim.Render(pos, wr.Palette(palette));
		}
	}
}
