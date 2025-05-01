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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.D2k.Graphics;
using OpenRA.Mods.D2k.Traits;

namespace OpenRA.Mods.D2k.ProjectileEffects
{
	[Desc("Renders a sonic post process effect. Attach " + nameof(SonicBlastRenderer) + "to the world actor.")]
	public sealed class SonicProjectileEffectInfo : IProjectileEffectInfo
	{
		IProjectileEffect IProjectileEffectInfo.Create(ProjectileArgs args, Func<WAngle> facing)
		{
			return new SonicProjectileEffect(args);
		}
	}

	public class SonicProjectileEffect : IProjectileEffect
	{
		readonly SonicBlastRenderer renderer;
		WPos pos;

		public SonicProjectileEffect(ProjectileArgs args)
		{
			pos = args.Source;
			renderer = args.SourceActor.World.WorldActor.Trait<SonicBlastRenderer>();
		}

		void IProjectileEffect.Tick(World world, WPos pos, WRot orientation)
		{
			this.pos = pos;
		}

		IEnumerable<IRenderable> IProjectileEffect.Render(WorldRenderer wr)
		{
			if (!wr.World.FogObscures(pos))
				return [(new SonicBlastRenderable(renderer, pos))];

			return SpriteRenderable.None;
		}

		void IProjectileEffect.Destroy(World world, WPos pos) { }
	}
}
