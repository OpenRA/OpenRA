#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Projectiles
{
	public class TeslaZapInfo : IProjectileInfo
	{
		public readonly string Image = "litning";

		[SequenceReference("Image")] public readonly string BrightSequence = "bright";
		[SequenceReference("Image")] public readonly string DimSequence = "dim";

		[PaletteReference] public readonly string Palette = "effect";

		public readonly int BrightZaps = 1;
		public readonly int DimZaps = 2;

		public readonly int Duration = 2;

		public readonly int DamageDuration = 1;

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
		[Sync] WPos target;

		public TeslaZap(TeslaZapInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;
			ticksUntilRemove = info.Duration;
			damageDuration = info.DamageDuration > info.Duration ? info.Duration : info.DamageDuration;
			target = args.PassiveTarget;

			var pos = args.GuidedTarget.IsValidFor(args.SourceActor) ? args.GuidedTarget.CenterPosition : args.PassiveTarget;
			zap = new TeslaZapRenderable(args.Source, 0, pos - args.Source,
				info.Image, info.BrightSequence, info.BrightZaps, info.DimSequence, info.DimZaps, info.Palette);
			var world = args.SourceActor.World;
			world.ScreenMap.Add(this, pos, zap.ScreenBounds(null));
		}

		public void Tick(World world)
		{
			if (ticksUntilRemove-- <= 0)
				world.AddFrameEndTask(w => w.Remove(this));

			// Zap tracks target
			if (info.TrackTarget && args.GuidedTarget.IsValidFor(args.SourceActor))
				target = args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.PositionClosestTo(args.Source);

			if (damageDuration-- > 0)
			{
				args.Weapon.Impact(Target.FromPos(target), args.SourceActor, args.DamageModifiers);
				world.ScreenMap.Update(this, target, zap.ScreenBounds(null));
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			zap = new TeslaZapRenderable(args.Source, 0, target - args.Source,
				info.Image, info.BrightSequence, info.BrightZaps, info.DimSequence, info.DimZaps, info.Palette);

			yield return zap;
		}
	}
}
