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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Effects
{
	public class InstantExplodeInfo : IProjectileInfo
	{
		public IProjectile Create(ProjectileArgs args) { return new InstantExplode(this, args); }
	}

	class InstantExplode : IProjectile
	{
		private ProjectileArgs args;

		public InstantExplode(InstantExplodeInfo instantExplodeInfo, ProjectileArgs args)
		{
			this.args = args;
		}

		public void Tick(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));

			args.Weapon.Impact(Target.FromPos(args.Source), args.SourceActor, args.DamageModifiers);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield break;
		}
	}
}
