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
using System.Drawing;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	[Desc("Not a sprite, but an engine effect.")]
	class LaserZapInfo : IProjectileInfo
	{
		public readonly int BeamWidth = 2;
		public readonly int BeamDuration = 10;
		public readonly bool UsePlayerColor = false;
		public readonly Color Color = Color.Red;
		public readonly string HitAnim = null;

		public IEffect Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.firedBy.Owner.Color.RGB : Color;
			return new LaserZap(args, this, c);
		}
	}

	class LaserZap : IEffect
	{
		ProjectileArgs args;
		LaserZapInfo info;
		int ticks = 0;
		Color color;
		bool doneDamage;
		Animation hitanim;

		public LaserZap(ProjectileArgs args, LaserZapInfo info, Color color)
		{
			this.args = args;
			this.info = info;
			this.color = color;

			if (info.HitAnim != null)
				this.hitanim = new Animation(info.HitAnim);
		}

		public void Tick(World world)
		{
			// Beam tracks target
			if (args.target.IsValid)
				args.dest = args.target.CenterLocation;

			if (!doneDamage)
			{
				if (hitanim != null)
					hitanim.PlayThen("idle",
						() => world.AddFrameEndTask(w => w.Remove(this)));
				Combat.DoImpacts(args);
				doneDamage = true;
			}
			++ticks;

			if (hitanim != null)
				hitanim.Tick();
			else
				if (ticks >= info.BeamDuration)
					world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
<<<<<<< HEAD
			if (hitanim != null)
				yield return new SpriteRenderable(hitanim.Image, args.dest.ToFloat2(),
=======
			if (explosion != null)
				yield return new SpriteRenderable(explosion.Image, args.dest.ToFloat2(),
>>>>>>> 93c89a6ef54d6d4155a890ed5be1392519be46d8
				                                  wr.Palette("effect"), (int)args.dest.Y);

			if (ticks >= info.BeamDuration)
				yield break;

			var rc = Color.FromArgb((info.BeamDuration - ticks)*255/info.BeamDuration, color);

			var src = new PPos(args.src.X, args.src.Y - args.srcAltitude);
			var dest = new PPos(args.dest.X, args.dest.Y - args.destAltitude);
			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.LineWidth = info.BeamWidth;
			wlr.DrawLine(src.ToFloat2(), dest.ToFloat2(), rc, rc);
			wlr.Flush();
			wlr.LineWidth = 1f;
		}
	}
}
