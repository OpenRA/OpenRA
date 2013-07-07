#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
		public IEffect Create(ProjectileArgs args) { return new GravityBomb(this, args); }
	}

	public class GravityBomb : IEffect
	{
		Animation anim;
		int2 dest;
		int altitude, destAltitude;
		ProjectileArgs Args;

		public GravityBomb(GravityBombInfo info, ProjectileArgs args)
		{
			Args = args;
			altitude = args.source.Z * Game.CellSize / 1024;
			destAltitude = args.passiveTarget.Z * Game.CellSize / 1024;
			dest = PPos.FromWPos(args.passiveTarget).ToInt2();

			anim = new Animation(info.Image);
			if (anim.HasSequence("open"))
				anim.PlayThen("open", () => anim.PlayRepeating("idle"));
			else
				anim.PlayRepeating("idle");
		}

		public void Tick(World world)
		{
			if (--altitude <= destAltitude)
			{
				world.AddFrameEndTask(w => w.Remove(this));
				Combat.DoImpacts(Args.passiveTarget, Args.sourceActor, Args.weapon, Args.firepowerModifier);
			}

			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var pos = dest - new int2(0, altitude) - .5f * anim.Image.size;
			yield return new SpriteRenderable(anim.Image, pos, wr.Palette("effect"), dest.Y);
		}
	}
}
