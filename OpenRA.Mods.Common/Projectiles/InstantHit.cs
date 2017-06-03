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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Simple, invisible, usually direct-on-target projectile.")]
	public class InstantHitInfo : IProjectileInfo, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Projectile can be blocked.")]
		public readonly bool Blockable = false;

		[Desc("The width of the projectile.")]
		public readonly WDist Width = new WDist(1);

		[Desc("Scan radius for actors with projectile-blocking trait. If set to zero (default), it will automatically scale",
			"to the blocker with the largest health shape. Only set custom values if you know what you're doing.")]
		public WDist BlockerScanRadius = WDist.Zero;

		public IProjectile Create(ProjectileArgs args) { return new InstantHit(this, args); }

		public void RulesetLoaded(Ruleset rules, WeaponInfo wi)
		{
			if (BlockerScanRadius == WDist.Zero)
				BlockerScanRadius = Util.MinimumRequiredBlockerScanRadius(rules);
		}
	}

	public class InstantHit : IProjectile
	{
		readonly ProjectileArgs args;
		readonly InstantHitInfo info;

		Target target;
		WPos source;

		public InstantHit(InstantHitInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;
			source = args.Source;

			if (info.Inaccuracy.Length > 0)
			{
				var inaccuracy = Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
				var maxOffset = inaccuracy * (args.PassiveTarget - source).Length / args.Weapon.Range.Length;
				target = Target.FromPos(args.PassiveTarget + WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxOffset / 1024);
			}
			else if (args.TrackClosestGuidedTargetPosition)
				target = Target.FromPos(args.GuidedTarget.Positions.PositionClosestTo(source));
			else
				target = args.GuidedTarget;
		}

		public void Tick(World world)
		{
			// Check for blocking actors
			WPos blockedPos;
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, source, target.CenterPosition,
				info.Width, info.BlockerScanRadius, out blockedPos))
			{
				target = Target.FromPos(blockedPos);
			}

			args.Weapon.Impact(target, args.SourceActor, args.DamageModifiers);
			world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return Enumerable.Empty<IRenderable>();
		}
	}
}
