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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Simple invisible direct on target projectile.")]
	public class InstantHitInfo : IProjectileInfo
	{
		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Projectile can be blocked.")]
		public readonly bool Blockable = false;

		[Desc("The width of the projectile.")]
		public readonly WDist Width = new WDist(1);

		[Desc("Extra search radius beyond projectile width. Required to ensure affecting actors with large health radius.")]
		public readonly WDist TargetExtraSearchRadius = new WDist(1536);

		public IProjectile Create(ProjectileArgs args) { return new InstantHit(this, args); }
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
			else
				target = args.GuidedTarget;
		}

		public void Tick(World world)
		{
			// Check for blocking actors
			WPos blockedPos;
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, source, target.CenterPosition,
				info.Width, info.TargetExtraSearchRadius, out blockedPos))
			{
				target = Target.FromPos(blockedPos);
			}

			args.Weapon.Impact(target, args.SourceActor, args.DamageModifiers);
			world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield break;
		}
	}
}
