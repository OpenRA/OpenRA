#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Simple invisible direct on target actor fake projectile.")]
	public class MeleeAttackInfo : IProjectileInfo
	{
		public IProjectile Create(ProjectileArgs args) { return new MeleeAttack(this, args); }
	}

	public class MeleeAttack : IProjectile
	{
		readonly ProjectileArgs args;

		bool doneDamage;

		public MeleeAttack(MeleeAttackInfo info, ProjectileArgs args)
		{
			this.args = args;
		}

		public void Tick(World world)
		{
			if (!doneDamage)
			{
				args.Weapon.Impact(args.GuidedTarget, args.SourceActor, args.DamageModifiers);
				world.AddFrameEndTask(w => w.Remove(this));
				doneDamage = true;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield break;
		}
	}
}
