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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Projectile with customisable acceleration vector.")]
	public class GravityBombInfo : IProjectileInfo
	{
		[Desc("Projectile movement vector per tick (forward, right, up), use negative values for opposite directions.")]
		public readonly WVec Velocity = WVec.Zero;

		[Desc("Value added to Velocity every tick.")]
		public readonly WVec Acceleration = new(0, 0, -15);

		public IProjectile Create(ProjectileArgs args) { return new GravityBomb(this, args); }
	}

	public class GravityBomb : IProjectile, ISync
	{
		readonly ProjectileArgs args;
		readonly WVec acceleration;

		WVec velocity;

		[Sync]
		WPos pos, lastPos;

		readonly IProjectileEffect[] effects;

		public GravityBomb(GravityBombInfo info, ProjectileArgs args)
		{
			this.args = args;
			pos = args.Source;
			var convertedVelocity = new WVec(info.Velocity.Y, -info.Velocity.X, info.Velocity.Z);
			velocity = convertedVelocity.Rotate(WRot.FromYaw(args.Facing));
			acceleration = new WVec(info.Acceleration.Y, -info.Acceleration.X, info.Acceleration.Z);

			effects = args.Weapon.ProjectileEffects.Select(c => c.Create(args, () => args.Facing)).ToArray();
		}

		public void Tick(World world)
		{
			lastPos = pos;
			pos += velocity;
			velocity += acceleration;

			var orientation = new WRot(WAngle.Zero, Util.GetVerticalAngle(lastPos, pos), args.Facing);
			if (pos.Z <= args.PassiveTarget.Z)
			{
				pos += new WVec(0, 0, args.PassiveTarget.Z - pos.Z);
				world.AddFrameEndTask(w => w.Remove(this));

				foreach (var c in effects)
					c.Destroy(world, pos);

				var warheadArgs = new WarheadArgs(args)
				{
					ImpactOrientation = orientation,
					ImpactPosition = pos,
				};

				args.Weapon.Impact(Target.FromPos(pos), warheadArgs);
			}

			foreach (var c in effects)
				c.Tick(world, pos, orientation);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			foreach (var c in effects)
				foreach (var r in c.Render(wr))
					yield return r;
		}
	}
}
