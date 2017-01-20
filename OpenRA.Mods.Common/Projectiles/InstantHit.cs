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
	[Desc("Simple invisible direct on target projectile.")]
	public class InstantHitInfo : IProjectileInfo, IRulesetLoaded
	{
		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Projectile can be blocked.")]
		public readonly bool Blockable = false;

		[Desc("The width of the projectile.")]
		public readonly WDist Width = new WDist(1);

		[Desc("Scan radius for victims beyond projectile width. If set to zero (default), it will automatically scale to the largest health shape.",
			"Custom overrides should not be necessary under normal circumstances.")]
		public WDist TargetExtraSearchRadius = WDist.Zero;

		public IProjectile Create(ProjectileArgs args) { return new InstantHit(this, args); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var validActors = rules.Actors.Where(a => a.Value.TraitInfos<HealthInfo>().Any()).ToList();

			// TODO: Make this handle multiple Health traits per actor
			var largestHealthRadius = validActors.Max(a => a.Value.TraitInfo<HealthInfo>().Shape.OuterRadius);

			if (TargetExtraSearchRadius == WDist.Zero)
				TargetExtraSearchRadius = largestHealthRadius;
		}
	}

	public class InstantHit : IProjectile
	{
		readonly ProjectileArgs args;
		readonly InstantHitInfo info;

		bool doneDamage;
		WPos target;
		WPos source;

		public InstantHit(InstantHitInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;
			target = args.PassiveTarget;
			source = args.Source;
		}

		public void Tick(World world)
		{
			// Check for blocking actors
			WPos blockedPos;
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, source, target,
				info.Width, info.TargetExtraSearchRadius, out blockedPos))
			{
				target = blockedPos;
			}

			if (info.Inaccuracy.Length > 0)
			{
				var inaccuracy = OpenRA.Mods.Common.Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
				var maxOffset = inaccuracy * (target - source).Length / args.Weapon.Range.Length;
				target += WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxOffset / 1024;
			}

			if (!doneDamage)
			{
				args.Weapon.Impact(Target.FromPos(target), args.SourceActor, args.DamageModifiers);
				doneDamage = true;
				world.AddFrameEndTask(w => w.Remove(this));
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield break;
		}
	}
}
