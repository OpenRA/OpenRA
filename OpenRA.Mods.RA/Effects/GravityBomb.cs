#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.RA.Effects
{
	public class GravityBombInfo : IProjectileInfo
	{
		public readonly string Image = null;
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

			anim = new Animation(info.Image);
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
				world.AddFrameEndTask(w => w.Remove(this));
				Combat.DoImpacts(args.PassiveTarget, args.SourceActor, args.Weapon, args.FirepowerModifier);
			}

			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(pos, wr.Palette("effect"));
		}
	}
}
