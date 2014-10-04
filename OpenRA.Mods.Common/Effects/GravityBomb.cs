#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Effects
{
	public class GravityBombInfo : IProjectileInfo
	{
		public readonly string Image = null;
		public readonly bool Shadow = false;
		public readonly WRange Velocity = WRange.Zero;
		public readonly WRange Acceleration = new WRange(15);

		public IEffect Create(ProjectileArgs args) { return new GravityBomb(this, args); }
	}

	public class GravityBomb : IEffect
	{
		GravityBombInfo info;
		Animation anim;
		ProjectileArgs args;
		WVec velocity;
		WPos pos;

		public GravityBomb(GravityBombInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			pos = args.Source;
			velocity = new WVec(WRange.Zero, WRange.Zero, -info.Velocity);

			anim = new Animation(args.SourceActor.World, info.Image);
			if (anim.HasSequence("open"))
				anim.PlayThen("open", () => anim.PlayRepeating("idle"));
			else
				anim.PlayRepeating("idle");
		}

		public void Tick(World world)
		{
			velocity -= new WVec(WRange.Zero, WRange.Zero, info.Acceleration);
			pos += velocity;

			if (pos.Z <= args.PassiveTarget.Z)
			{
				pos += new WVec(0, 0, args.PassiveTarget.Z - pos.Z);
				world.AddFrameEndTask(w => w.Remove(this));
				args.Weapon.Impact(Target.FromPos(pos), args.SourceActor, args.DamageModifiers);
			}

			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var cell = wr.world.Map.CellContaining(pos);
			if (!args.SourceActor.World.FogObscures(cell))
			{
				if (info.Shadow)
				{
					var shadowPos = pos - new WVec(0, 0, pos.Z);
					foreach (var r in anim.Render(shadowPos, wr.Palette("shadow")))
						yield return r;
				}

				var palette = wr.Palette(args.Weapon.Palette);
				foreach (var r in anim.Render(pos, palette))
					yield return r;
			}
		}
	}
}
