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

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.ProjectileEffects
{
	[Desc("Renders a chrono vortex post process effect. Attach " + nameof(ChronoVortexRenderer) + "to the world actor.")]
	public sealed class ChronoVortexProjectileEffectInfo : IProjectileEffectInfo
	{
		[Desc("The projectile effect will end with the vortex closing animation.")]
		public bool EndAnimation = true;

		IProjectileEffect IProjectileEffectInfo.Create(ProjectileArgs args, Func<WAngle> facing)
		{
			return new ChronoVortexProjectileEffect(args, this);
		}
	}

	public class ChronoVortexProjectileEffect : IProjectileEffect
	{
		readonly ChronoVortexRenderer renderer;
		readonly ChronoVortexProjectileEffectInfo info;
		WPos pos;
		int frame;

		public ChronoVortexProjectileEffect(ProjectileArgs args, ChronoVortexProjectileEffectInfo info)
		{
			this.info = info;
			pos = args.Source;
			renderer = args.SourceActor.World.WorldActor.Trait<ChronoVortexRenderer>();
		}

		void IProjectileEffect.Tick(World world, WPos pos, WRot facing)
		{
			// First 16 frames are the vortex opening
			// Next 16 frames are loopable
			// Final 16 frames are the vortex closing
			this.pos = pos;
			if (++frame == 32)
				frame = 16;
		}

		IEnumerable<IRenderable> IProjectileEffect.Render(WorldRenderer wr)
		{
			if (!wr.World.FogObscures(pos))
				return [new ChronoVortexRenderable(renderer, pos, frame)];

			return SpriteRenderable.None;
		}

		void IProjectileEffect.Destroy(World world, WPos pos)
		{
			if (info.EndAnimation)
				world.AddFrameEndTask(w => w.Add(new ChronoVortex(world, frame, pos, renderer)));
		}
	}

	sealed class ChronoVortex : IEffect, ISpatiallyPartitionable
	{
		public static readonly Size Size = new(64, 64);

		readonly ChronoVortexRenderer renderer;
		readonly WPos pos;
		int frame;

		public ChronoVortex(World world, int frame, WPos pos, ChronoVortexRenderer renderer)
		{
			this.frame = frame;
			this.renderer = renderer;
			this.pos = pos;

			world.ScreenMap.Add(this, pos, Size);
		}

		public void Tick(World world)
		{
			if (++frame == 48)
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield return new ChronoVortexRenderable(renderer, pos, frame);
		}
	}
}
