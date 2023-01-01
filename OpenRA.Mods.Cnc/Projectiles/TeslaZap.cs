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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Projectiles
{
	[Desc("Instant-hit projectile used to create electricity-like effects.")]
	public class TeslaZapInfo : IProjectileInfo
	{
		public readonly string Image = "litning";

		[SequenceReference(nameof(Image))]
		[Desc("Sprite sequence to play at the center.")]
		public readonly string BrightSequence = "bright";

		[SequenceReference(nameof(Image))]
		[Desc("Sprite sequence to play at the borders.")]
		public readonly string DimSequence = "dim";

		[PaletteReference]
		[Desc("The palette used to draw this electric zap.")]
		public readonly string Palette = "effect";

		[Desc("How many sprite sequences to play at the center.")]
		public readonly int BrightZaps = 1;

		[Desc("How many sprite sequences to play at the borders.")]
		public readonly int DimZaps = 2;

		[Desc("How long (in ticks) to play the sprite sequences.")]
		public readonly int Duration = 2;

		[Desc("How long (in ticks) until applying damage. Can't be longer than `" + nameof(Duration) + "`")]
		public readonly int DamageDuration = 1;

		[Desc("Follow the targeted actor when it moves.")]
		public readonly bool TrackTarget = true;

		public IProjectile Create(ProjectileArgs args) { return new TeslaZap(this, args); }
	}

	public class TeslaZap : IProjectile, ISync
	{
		readonly ProjectileArgs args;
		readonly TeslaZapInfo info;
		TeslaZapRenderable zap;
		int ticksUntilRemove;
		int damageDuration;

		[Sync]
		WPos target;

		public TeslaZap(TeslaZapInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;
			ticksUntilRemove = info.Duration;
			damageDuration = info.DamageDuration > info.Duration ? info.Duration : info.DamageDuration;
			target = args.PassiveTarget;
		}

		public void Tick(World world)
		{
			if (ticksUntilRemove-- <= 0)
				world.AddFrameEndTask(w => w.Remove(this));

			// Zap tracks target
			if (info.TrackTarget && args.GuidedTarget.IsValidFor(args.SourceActor))
				target = args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.PositionClosestTo(args.Source);

			if (damageDuration-- > 0)
				args.Weapon.Impact(Target.FromPos(target), new WarheadArgs(args));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			zap = new TeslaZapRenderable(args.Source, 0, target - args.Source,
				info.Image, info.BrightSequence, info.BrightZaps, info.DimSequence, info.DimZaps, info.Palette);

			yield return zap;
		}
	}
}
