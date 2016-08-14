#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Projectiles
{
	[Desc("Dummy projectile exploding on/above the target actor/position after a specified delay.")]
	public class AthenaProjectileInfo : IProjectileInfo
	{
		[Desc("Explosion altitude added to target actor.")]
		public readonly WDist Altitude = WDist.Zero;

		[Desc("Delay between firing and exploding.")]
		public readonly int Delay = 0;

		public IProjectile Create(ProjectileArgs args) { return new AthenaProjectile(this, args); }
	}

	class AthenaProjectile : IProjectile
	{
		readonly ProjectileArgs args;
		readonly WDist altitude;

		int delay;

		public AthenaProjectile(AthenaProjectileInfo info, ProjectileArgs args)
		{
			this.args = args;
			altitude = info.Altitude;
			delay = info.Delay;
		}

		public void Tick(World world)
		{
			if (--delay < 0)
			{
				WPos target;
				if (args.GuidedTarget.IsValidFor(args.SourceActor))
					target = args.GuidedTarget.CenterPosition + new WVec(WDist.Zero, WDist.Zero, altitude);
				else
					target = args.PassiveTarget + new WVec(WDist.Zero, WDist.Zero, altitude);

				world.AddFrameEndTask(w => w.Remove(this));

				args.Weapon.Impact(Target.FromPos(target), args.SourceActor, args.DamageModifiers);
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield break;
		}
	}
}
