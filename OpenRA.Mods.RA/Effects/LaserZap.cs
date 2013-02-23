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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class LaserZapInfo : IProjectileInfo
	{
		public readonly int BeamRadius = 1;
		public readonly int BeamDuration = 10;
		public readonly bool UsePlayerColor = false;
		public readonly Color Color = Color.Red;
		public readonly string Explosion = "laserfire";

		public IEffect Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.firedBy.Owner.ColorRamp.GetColor(0) : Color;
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
		Animation explosion;

		public LaserZap(ProjectileArgs args, LaserZapInfo info, Color color)
		{
			this.args = args;
			this.info = info;
			this.color = color;

			if (info.Explosion != null)
				this.explosion = new Animation(info.Explosion);
		}

		public void Tick(World world)
		{
			// Beam tracks target
			if (args.target.IsValid)
				args.dest = args.target.CenterLocation;

			if (!doneDamage)
			{
				if (explosion != null)
					explosion.PlayThen("idle",
						() => world.AddFrameEndTask(w => w.Remove(this)));
				Combat.DoImpacts(args);
				doneDamage = true;
			}
			++ticks;

			if (explosion != null)
				explosion.Tick();
			else
				if (ticks >= info.BeamDuration)
					world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			if (explosion != null)
				yield return new Renderable(explosion.Image, args.dest.ToFloat2() - .5f * explosion.Image.size,
				                            wr.Palette("effect"), (int)args.dest.Y);

			if (ticks >= info.BeamDuration)
				yield break;

			var rc = Color.FromArgb((info.BeamDuration - ticks)*255/info.BeamDuration, color);

			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.LineWidth = info.BeamRadius * 2;
			wlr.DrawLine(args.src.ToFloat2(), args.dest.ToFloat2(), rc, rc);
			wlr.Flush();
			wlr.LineWidth = 1f;
		}
	}
}
