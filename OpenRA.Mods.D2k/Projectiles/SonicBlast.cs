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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2k.Graphics;
using OpenRA.Mods.D2k.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Projectiles
{
	[Desc("Blast projectile that travels in a straight line.")]
	public class SonicBlastInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate a randomly picked velocity per blast.")]
		public readonly WDist[] Speed = [new(128)];

		[Desc("The number of ticks between the blast causing warhead impacts in its area of effect.")]
		public readonly int DamageInterval = 1;

		[Desc("The minimum distance the blast travels.")]
		public readonly WDist MinDistance = WDist.Zero;

		[Desc("Width of projectile (used for finding blocking actors).")]
		public readonly WDist Width = new(650);

		[Desc("Damage modifier applied at each range step.")]
		public readonly int[] Falloff = [100, 100];

		[Desc("Ranges at which each Falloff step is defined.")]
		public readonly WDist[] Range = [WDist.Zero, new(int.MaxValue)];

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are" +
		"'Maximum' - scale from 0 to max with range," +
		"'PerCellIncrement' - scale from 0 with range" +
		"'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Can this projectile be blocked when hitting actors with an nameof(BlocksProjectiles) trait.")]
		public readonly bool Blockable = false;

		public IProjectile Create(ProjectileArgs args)
		{
			return new SonicBlast(this, args);
		}
	}

	public class SonicBlast : IProjectile, ISync
	{
		readonly SonicBlastInfo info;
		readonly ProjectileArgs args;

		readonly WDist speed;
		readonly SonicBlastRenderer renderer;

		[Sync]
		WPos pos, lastPos;
		readonly WPos target;
		int length;

		int ticks;

		public SonicBlast(SonicBlastInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			var world = args.SourceActor.World;
			renderer = world.WorldActor.Trait<SonicBlastRenderer>();

			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];
			pos = args.Source;
			target = args.PassiveTarget;
			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Common.Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(world.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw((target - pos).Yaw));
			var dist = (args.SourceActor.CenterPosition - target).Length;
			var extraDist = 0;
			if (info.MinDistance.Length > dist)
				extraDist = info.MinDistance.Length - dist;
			target += dir * extraDist / 1024;
			length = Math.Max((target - pos).Length / speed.Length, 1);
		}

		public void Tick(World world)
		{
			if (ticks++ >= length)
				world.AddFrameEndTask(w => w.Remove(this));

			lastPos = pos;
			pos = WPos.LerpQuadratic(args.Source, target, WAngle.Zero, ticks, length);

			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, args.SourceActor.Owner, lastPos, pos, info.Width, out var blockedPos))
			{
				pos = blockedPos;
				length = Math.Min(ticks, length);
			}

			if (ticks % info.DamageInterval == 0)
			{
				var adjustedModifiers = args.DamageModifiers.Append(GetFalloff((args.Source - pos).Length));
				var warheadArgs = new WarheadArgs(args)
				{
					ImpactOrientation = new WRot(WAngle.Zero, Common.Util.GetVerticalAngle(args.Source, target), args.CurrentMuzzleFacing()),
					ImpactPosition = pos,
					DamageModifiers = adjustedModifiers.ToArray(),
				};
				args.Weapon.Impact(Target.FromPos(pos), warheadArgs);
			}
		}

		int GetFalloff(int distance)
		{
			var inner = info.Range[0].Length;
			for (var i = 1; i < info.Range.Length; i++)
			{
				var outer = info.Range[i].Length;
				if (outer > distance)
					return int2.Lerp(info.Falloff[i - 1], info.Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!wr.World.FogObscures(pos))
				return [(new SonicBlastRenderable(renderer, pos))];

			return SpriteRenderable.None;
		}
	}
}
