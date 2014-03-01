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

namespace OpenRA.Mods.RA.Effects
{
	public class GravityBombInfo : IProjectileInfo
	{
		public readonly string Image = null;
		public readonly WDist Velocity = WDist.Zero;
		public readonly WDist Acceleration = new WDist(15);

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
			velocity = new WVec(WDist.Zero, WDist.Zero, -info.Velocity);

			anim = new Animation(info.Image);
			if (anim.HasSequence("open"))
				anim.PlayThen("open", () => anim.PlayRepeating("idle"));
			else
				anim.PlayRepeating("idle");
		}

		public void Tick(World world)
		{
			velocity -= new WVec(WDist.Zero, WDist.Zero, info.Acceleration);
			pos += velocity;

			if (pos.Z <= args.PassiveTarget.Z)
			{
				pos += new WVec(0, 0, args.PassiveTarget.Z - pos.Z);
				world.AddFrameEndTask(w => w.Remove(this));
				Combat.DoImpacts(pos, args.SourceActor, args.Weapon, args.FirepowerModifier);
			}

			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(pos, wr.Palette("effect"));
		}
	}
}
