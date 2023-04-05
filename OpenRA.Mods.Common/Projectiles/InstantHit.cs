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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Instant, invisible, usually direct-on-target projectile.")]
	public class InstantHitInfo : IProjectileInfo
	{
		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are 'Maximum' - scale from 0 to max with range, 'PerCellIncrement' - scale from 0 with range and 'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Projectile can be blocked.")]
		public readonly bool Blockable = false;

		[Desc("The width of the projectile.")]
		public readonly WDist Width = new(1);

		[Desc("Scan radius for actors with projectile-blocking trait. If set to a negative value (default), it will automatically scale",
			"to the blocker with the largest health shape. Only set custom values if you know what you're doing.")]
		public readonly WDist BlockerScanRadius = new(-1);

		public IProjectile Create(ProjectileArgs args) { return new InstantHit(this, args); }
	}

	public class InstantHit : IProjectile
	{
		readonly ProjectileArgs args;
		readonly InstantHitInfo info;

		Target target;

		public InstantHit(InstantHitInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;

			if (args.Weapon.TargetActorCenter)
				target = args.GuidedTarget;
			else if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				var inaccuracyOffset = WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxInaccuracyOffset / 1024;
				target = Target.FromPos(args.PassiveTarget + inaccuracyOffset);
			}
			else
				target = Target.FromPos(args.PassiveTarget);
		}

		public void Tick(World world)
		{
			// If GuidedTarget has become invalid due to getting killed the same tick,
			// we need to set target to args.PassiveTarget to prevent target.CenterPosition below from crashing.
			if (target.Type == TargetType.Invalid)
				target = Target.FromPos(args.PassiveTarget);

			// Check for blocking actors
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, args.SourceActor.Owner, args.Source, target.CenterPosition, info.Width, out var blockedPos))
				target = Target.FromPos(blockedPos);

			var warheadArgs = new WarheadArgs(args)
			{
				ImpactOrientation = new WRot(WAngle.Zero, Util.GetVerticalAngle(args.Source, target.CenterPosition), args.Facing),
				ImpactPosition = target.CenterPosition,
			};

			args.Weapon.Impact(target, warheadArgs);
			world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return Enumerable.Empty<IRenderable>();
		}
	}
}
