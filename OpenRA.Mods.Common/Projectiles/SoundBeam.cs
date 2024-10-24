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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Beam projectile that travels in a straight line.")]
	public class SoundBeamInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate a randomly picked velocity per beam.")]
		public readonly WDist[] Speed = { new(128) };

		[Desc("The number of ticks between the beam causing warhead impacts in its area of effect.")]
		public readonly int DamageInterval = 3;

		[Desc("The beam Diameter")]
		public readonly WDist Radius = new(256);

		[Desc("How far beyond the target the projectile keeps on travelling.")]
		public readonly WDist BeyondTargetRange = new(0);

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("The minimum distance the beam travels.")]
		public readonly WDist MinDistance = WDist.Zero;

		[Desc("Damage modifier applied at each range step.")]
		public readonly int[] Falloff = { 100, 100 };

		[Desc("Ranges at which each Falloff step is defined.")]
		public readonly WDist[] Range = { WDist.Zero, new(int.MaxValue) };

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are 'Maximum' - scale from 0 to max with range, 'PerCellIncrement' - scale from 0 with range and 'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Can this projectile be blocked when hitting actors with an IBlocksProjectiles trait.")]
		public readonly bool Blockable = false;

		[Desc("Does the beam follow the target.")]
		public readonly bool TrackTarget = false;

		[Desc("Color of the beam.")]
		public readonly Color Color = Color.SkyBlue;

		public IProjectile Create(ProjectileArgs args)
		{
			return new SoundBeam(this, args, Color);
		}
	}

	public class SoundBeam : IProjectile, ISync
	{
		readonly SoundBeamInfo info;
		readonly ProjectileArgs args;

		readonly WDist speed;

		[Sync]
		WPos pos, lastPos;
		readonly WPos target;
		int length;

		int ticks;

		protected bool FlightLengthReached => ticks >= length;

		public SoundBeam(SoundBeamInfo info, ProjectileArgs args, Color color)
		{
			this.info = info;
			this.args = args;
			var world = args.SourceActor.World;
			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];
			pos = args.Source;
			target = args.PassiveTarget;
			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(world.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw((target - pos).Yaw));
			var dist = (args.SourceActor.CenterPosition - target).Length;
			int extraDist;
			if (info.MinDistance.Length > dist)
			{
				if (info.MinDistance.Length - dist < info.BeyondTargetRange.Length)
					extraDist = info.BeyondTargetRange.Length;
				else
					extraDist = info.MinDistance.Length - dist;
			}
			else
				extraDist = info.BeyondTargetRange.Length;

			target += dir * extraDist / 1024;
			length = Math.Max((target - pos).Length / speed.Length, 1);
		}

		public void Tick(World world)
		{
			if (ticks++ >= length)
			{
				pos = target;
				world.AddFrameEndTask(w => w.Remove(this));
			}

			lastPos = pos;
			pos = WPos.LerpQuadratic(args.Source, target, WAngle.Zero, ticks, length);

			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, args.SourceActor.Owner, lastPos, pos, info.Radius, out var blockedPos))
			{
				pos = blockedPos;
				length = Math.Min(ticks, length);
			}

			if (ticks % info.DamageInterval == 0)
			{
				var adjustedModifiers = args.DamageModifiers.Append(GetFalloff((args.Source - pos).Length));
				var warheadArgs = new WarheadArgs(args)
				{
					ImpactOrientation = new WRot(WAngle.Zero, Util.GetVerticalAngle(args.Source, target), args.CurrentMuzzleFacing()),
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
			{
				var beamRender = new BeamCircle(pos, info.Radius, 1, info.Color, true);
				return new[] { (IRenderable)beamRender };
			}

			return SpriteRenderable.None;
		}
	}
}
