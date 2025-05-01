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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Projectile that travels in a straight line or arc.")]
	public class BulletInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly WDist[] Speed = [new(17)];

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are " +
			"'Maximum' - scale from 0 to max with range, " +
			"'PerCellIncrement' - scale from 0 with range, " +
			"'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Is this blocked by actors with BlocksProjectiles trait.")]
		public readonly bool Blockable = true;

		[Desc("Width of projectile (used for finding blocking actors).")]
		public readonly WDist Width = new(1);

		[Desc("Arc in WAngles, two values indicate variable arc.")]
		public readonly WAngle[] LaunchAngle = [WAngle.Zero];

		[Desc("Up to how many times does this bullet bounce when touching ground without hitting a target.",
			"0 implies exploding on contact with the originally targeted position.")]
		public readonly int BounceCount = 0;

		[Desc("Modify distance of each bounce by this percentage of previous distance.")]
		public readonly int BounceRangeModifier = 60;

		[Desc("Sound to play when the projectile hits the ground, but not the target.")]
		public readonly string BounceSound = null;

		[Desc("Terrain where the projectile explodes instead of bouncing.")]
		public readonly HashSet<string> InvalidBounceTerrain = [];

		[Desc("Trigger the explosion if the projectile touches an actor thats owner has these player relationships.")]
		public readonly PlayerRelationship ValidBounceBlockerRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral;

		[Desc("Altitude above terrain below which to explode. Zero effectively deactivates airburst.")]
		public readonly WDist AirburstAltitude = WDist.Zero;

		public virtual IProjectile Create(ProjectileArgs args) { return new Bullet(this, args); }
	}

	public class Bullet : IProjectile, ISync
	{
		readonly BulletInfo info;
		protected readonly ProjectileArgs Args;
		readonly WAngle facing;
		readonly WAngle angle;
		readonly WDist speed;

		[Sync]
		protected WPos pos, lastPos, target, source;

		int length;
		int ticks;
		int remainingBounces;

		protected bool FlightLengthReached => ticks >= length;

		readonly IProjectileEffect[] effects;

		public Bullet(BulletInfo info, ProjectileArgs args)
		{
			this.info = info;
			Args = args;
			pos = args.Source;
			source = args.Source;

			var world = args.SourceActor.World;

			if (info.LaunchAngle.Length > 1)
				angle = new WAngle(world.SharedRandom.Next(info.LaunchAngle[0].Angle, info.LaunchAngle[1].Angle));
			else
				angle = info.LaunchAngle[0];

			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];

			target = args.PassiveTarget;
			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(world.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			if (info.AirburstAltitude > WDist.Zero)
				target += new WVec(WDist.Zero, WDist.Zero, info.AirburstAltitude);

			facing = (target - pos).Yaw;
			length = Math.Max((target - pos).Length / speed.Length, 1);

			remainingBounces = info.BounceCount;

			effects = args.Weapon.ProjectileEffects.Select(c => c.Create(args, GetEffectiveFacing)).ToArray();
		}

		WAngle GetEffectiveFacing()
		{
			var at = (float)ticks / (length - 1);
			var attitude = angle.Tan() * (1 - 2 * at) / (4 * 1024);

			var u = facing.Angle % 512 / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		public virtual void Tick(World world)
		{
			lastPos = pos;
			pos = WPos.LerpQuadratic(source, target, angle, ticks, length);

			var orientation = new WRot(WAngle.Zero, Util.GetVerticalAngle(lastPos, pos), Args.Facing);
			foreach (var c in effects)
				c.Tick(world, pos, orientation);

			if (ShouldExplode(world))
			{
				foreach (var c in effects)
					c.Destroy(world, pos);

				Explode(world, orientation);
			}
		}

		bool ShouldExplode(World world)
		{
			// Check for walls or other blocking obstacles
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, Args.SourceActor.Owner, lastPos, pos, info.Width, out var blockedPos))
			{
				pos = blockedPos;
				return true;
			}

			var flightLengthReached = ticks++ >= length;
			var shouldBounce = remainingBounces > 0;

			if (flightLengthReached && shouldBounce)
			{
				var cell = world.Map.CellContaining(pos);
				if (!world.Map.Contains(cell))
					return true;

				if (info.InvalidBounceTerrain.Contains(world.Map.GetTerrainInfo(cell).Type))
					return true;

				if (AnyValidTargetsInRadius(world, pos, info.Width, Args.SourceActor, true))
					return true;

				target += (pos - source) * info.BounceRangeModifier / 100;
				var dat = world.Map.DistanceAboveTerrain(target);
				target += new WVec(0, 0, -dat.Length);
				length = Math.Max((target - pos).Length / speed.Length, 1);

				ticks = 0;
				source = pos;
				Game.Sound.Play(SoundType.World, info.BounceSound, source);
				remainingBounces--;
			}

			// Flight length reached / exceeded
			if (flightLengthReached && !shouldBounce)
				return true;

			// Driving into cell with higher height level
			if (world.Map.DistanceAboveTerrain(pos).Length < 0)
				return true;

			// After first bounce, check for targets each tick
			if (remainingBounces < info.BounceCount && AnyValidTargetsInRadius(world, pos, info.Width, Args.SourceActor, true))
				return true;

			return false;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (FlightLengthReached)
				yield break;

			foreach (var c in effects)
				foreach (var r in c.Render(wr))
					yield return r;
		}

		protected virtual void Explode(World world, WRot orientation)
		{
			world.AddFrameEndTask(w => w.Remove(this));

			foreach (var c in effects)
				c.Destroy(world, pos);

			var warheadArgs = new WarheadArgs(Args)
			{
				ImpactOrientation = orientation,
				ImpactPosition = pos,
			};

			Args.Weapon.Impact(Target.FromPos(pos), warheadArgs);
		}

		bool AnyValidTargetsInRadius(World world, WPos pos, WDist radius, Actor firedBy, bool checkTargetType)
		{
			foreach (var victim in world.FindActorsOnCircle(pos, radius))
			{
				if (checkTargetType && !Target.FromActor(victim).IsValidFor(firedBy))
					continue;

				if (victim != Args.GuidedTarget.Actor && !info.ValidBounceBlockerRelationships.HasRelationship(firedBy.Owner.RelationshipWith(victim.Owner)))
					continue;

				// If the impact position is within any actor's HitShape, we have a direct hit
				// PERF: Avoid using TraitsImplementing<HitShape> that needs to find the actor in the trait dictionary.
				foreach (var targetPos in victim.EnabledTargetablePositions)
					if (targetPos is HitShape h && h.DistanceFromEdge(victim, pos).Length <= 0)
						return true;
			}

			return false;
		}
	}
}
