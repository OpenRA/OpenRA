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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.ProjectileEffects
{
	[Desc("Emitts a trail of sprites.")]
	public sealed class EmitterProjectileEffectInfo : IProjectileEffectInfo
	{
		[FieldLoader.Require]
		[Desc("Trail animation.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Loop a randomly chosen sequence of TrailImage from this list while this projectile is moving.")]
		public readonly string[] Sequences = ["idle"];

		[Desc("Interval in ticks between each spawned Trail animation.")]
		public readonly int Interval = 2;

		[Desc("Delay in ticks until trail animation is spawned.")]
		public readonly int Delay = 1;

		[PaletteReference(nameof(UsePlayerPalette))]
		[Desc("Palette used to render the trail sequence.")]
		public readonly string Palette = "effect";

		[Desc("Use the Player Palette to render the trail sequence.")]
		public readonly bool UsePlayerPalette = false;

		IProjectileEffect IProjectileEffectInfo.Create(ProjectileArgs args, Func<WAngle> facing)
		{
			return new EmitterProjectileEffect(args, this);
		}
	}

	public sealed class EmitterProjectileEffect : IProjectileEffect
	{
		readonly EmitterProjectileEffectInfo info;
		int ticks;
		readonly string trailPalette;

		public EmitterProjectileEffect(ProjectileArgs args, EmitterProjectileEffectInfo info)
		{
			this.info = info;
			ticks = info.Delay;

			trailPalette = info.Palette;
			if (info.UsePlayerPalette)
				trailPalette += args.SourceActor.Owner.InternalName;
		}

		void IProjectileEffect.Tick(World world, WPos pos, WRot orientation)
		{
			if (--ticks >= 0)
				return;

			world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, w,
				info.Image, info.Sequences.Random(world.SharedRandom), trailPalette, delay: info.Delay)));

			ticks = info.Interval;
		}

		IEnumerable<IRenderable> IProjectileEffect.Render(WorldRenderer wr) { return SpriteRenderable.None; }

		void IProjectileEffect.Destroy(World world, WPos pos) { }
	}
}
