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
	public class AreaBeamInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate a randomly picked velocity per beam.")]
		public readonly WDist[] Speed = { new(128) };

		[Desc("The maximum duration (in ticks) of each beam burst.")]
		public readonly int Duration = 10;

		[Desc("The number of ticks between the beam causing warhead impacts in its area of effect.")]
		public readonly int DamageInterval = 3;

		[Desc("The width of the beam.")]
		public readonly WDist Width = new(512);

		[Desc("The shape of the beam.  Accepts values Cylindrical or Flat.")]
		public readonly BeamRenderableShape Shape = BeamRenderableShape.Cylindrical;

		[Desc("How far beyond the target the projectile keeps on travelling.")]
		public readonly WDist BeyondTargetRange = new(0);

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

		[Desc("Should the beam be visually rendered? False = Beam is invisible.")]
		public readonly bool RenderBeam = true;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("Color of the beam.")]
		public readonly Color Color = Color.Red;

		[Desc("Beam color is the player's color.")]
		public readonly bool UsePlayerColor = false;

		public IProjectile Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.SourceActor.OwnerColor() : Color;
			return new AreaBeam(this, args, c);
		}
	}

	public class AreaBeam : IProjectile, ISync
	{
		readonly AreaBeamInfo info;
		readonly ProjectileArgs args;
		readonly AttackBase actorAttackBase;
		readonly Color color;
		readonly WDist speed;
		readonly WDist weaponRange;

		[Sync]
		WPos headPos;

		[Sync]
		WPos tailPos;

		[Sync]
		WPos target;

		int length;
		WAngle towardsTargetFacing;
		int headTicks;
		int tailTicks;
		bool isHeadTravelling = true;
		bool isTailTravelling;
		bool continueTracking = true;

		bool IsBeamComplete => !isHeadTravelling && headTicks >= length && !isTailTravelling && tailTicks >= length;

		public AreaBeam(AreaBeamInfo info, ProjectileArgs args, Color color)
		{
			this.info = info;
			this.args = args;
			this.color = color;
			actorAttackBase = args.SourceActor.Trait<AttackBase>();

			var world = args.SourceActor.World;
			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];

			// Both the head and tail start at the source actor, but initially only the head is travelling.
			headPos = args.Source;
			tailPos = headPos;

			target = args.PassiveTarget;
			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(world.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			towardsTargetFacing = (target - headPos).Yaw;

			// Update the target position with the range we shoot beyond the target by
			// I.e. we can deliberately overshoot, so aim for that position
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(towardsTargetFacing));
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

			length = Math.Max((target - headPos).Length / speed.Length, 1);
			weaponRange = new WDist(Util.ApplyPercentageModifiers(args.Weapon.Range.Length, args.RangeModifiers));
		}

		void TrackTarget()
		{
			if (!continueTracking)
				return;

			if (args.GuidedTarget.IsValidFor(args.SourceActor))
			{
				var guidedTargetPos = args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.ClosestToIgnoringPath(args.Source);
				var targetDistance = new WDist((guidedTargetPos - args.Source).Length);

				// Only continue tracking target if it's within weapon range +
				// BeyondTargetRange to avoid edge case stuttering (start firing and immediately stop again).
				if (targetDistance > weaponRange + info.BeyondTargetRange)
					StopTargeting();
				else
				{
					target = guidedTargetPos;
					towardsTargetFacing = (target - args.Source).Yaw;

					// Update the target position with the range we shoot beyond the target by
					// I.e. we can deliberately overshoot, so aim for that position
					var dir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(towardsTargetFacing));
					target += dir * info.BeyondTargetRange.Length / 1024;
				}
			}
		}

		void StopTargeting()
		{
			continueTracking = false;
			isTailTravelling = true;
		}

		public void Tick(World world)
		{
			if (info.TrackTarget)
				TrackTarget();

			if (++headTicks >= length)
			{
				headPos = target;
				isHeadTravelling = false;
			}
			else if (isHeadTravelling)
				headPos = WPos.LerpQuadratic(args.Source, target, WAngle.Zero, headTicks, length);

			if (tailTicks <= 0 && args.SourceActor.IsInWorld && !args.SourceActor.IsDead)
			{
				args.Source = args.CurrentSource();
				tailPos = args.Source;
			}

			// Allow for leniency to avoid edge case stuttering (start firing and immediately stop again).
			var outOfWeaponRange = weaponRange + info.BeyondTargetRange < new WDist((args.PassiveTarget - args.Source).Length);

			// While the head is travelling, the tail must start to follow Duration ticks later.
			// Alternatively, also stop emitting the beam if source actor dies or is ordered to stop.
			if ((headTicks >= info.Duration && !isTailTravelling) || args.SourceActor.IsDead ||
				!actorAttackBase.IsAiming || outOfWeaponRange)
				StopTargeting();

			if (isTailTravelling)
			{
				if (++tailTicks >= length)
				{
					tailPos = target;
					isTailTravelling = false;
				}
				else
					tailPos = WPos.LerpQuadratic(args.Source, target, WAngle.Zero, tailTicks, length);
			}

			// Check for blocking actors
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, args.SourceActor.Owner, tailPos, headPos, info.Width, out var blockedPos))
			{
				headPos = blockedPos;
				target = headPos;
				length = Math.Min(headTicks, length);
			}

			// Damage is applied to intersected actors every DamageInterval ticks
			if (headTicks % info.DamageInterval == 0)
			{
				var actors = world.FindActorsOnLine(tailPos, headPos, info.Width);
				foreach (var a in actors)
				{
					var adjustedModifiers = args.DamageModifiers.Append(GetFalloff((args.Source - a.CenterPosition).Length));

					var warheadArgs = new WarheadArgs(args)
					{
						ImpactOrientation = new WRot(WAngle.Zero, Util.GetVerticalAngle(args.Source, target), args.CurrentMuzzleFacing()),

						// Calculating an impact position is bogus for line damage.
						// FindActorsOnLine guarantees that the beam touches the target's HitShape,
						// so we just assume a center hit to avoid bogus warhead recalculations.
						ImpactPosition = a.CenterPosition,
						DamageModifiers = adjustedModifiers.ToArray(),
					};

					args.Weapon.Impact(Target.FromActor(a), warheadArgs);
				}
			}

			if (IsBeamComplete)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!IsBeamComplete && info.RenderBeam && !(wr.World.FogObscures(tailPos) && wr.World.FogObscures(headPos)))
			{
				var beamRender = new BeamRenderable(headPos, info.ZOffset, tailPos - headPos, info.Shape, info.Width, color);
				return new[] { (IRenderable)beamRender };
			}

			return SpriteRenderable.None;
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
	}
}
