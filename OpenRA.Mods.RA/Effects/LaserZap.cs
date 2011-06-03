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
		public readonly string Explosion = "laserfire";

		public IEffect Create(ProjectileArgs args) 
		{
			Color c = UsePlayerColor ? args.firedBy.Owner.ColorRamp.GetColor(0) : Color.Red;
			return new LaserZap(args, BeamRadius, c, BeamDuration, Explosion);
		}
	}

	class LaserZap : IEffect
	{
		ProjectileArgs args;
		readonly int radius;
		int ticks = 0;
		int beamTicks; // Duration of beam
		Color color;
		bool doneDamage = false;
		Animation explosion;
		
		public LaserZap(ProjectileArgs args, int radius, Color color, int beamTicks, string explosion)
		{
			this.args = args;
			this.color = color;
			this.radius = radius;
			this.beamTicks = beamTicks;
			this.explosion = new Animation(explosion);
		}

		public void Tick(World world)
		{
			// Beam tracks target
			if (args.target.IsValid)
				args.dest = args.target.CenterLocation;

			if (!doneDamage)
			{
				explosion.PlayThen("idle",
					() => world.AddFrameEndTask(w => w.Remove(this)));
				Combat.DoImpacts(args);
				doneDamage = true;
			}
			++ticks;
			explosion.Tick();
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(explosion.Image, args.dest - .5f * explosion.Image.size, "effect", (int)args.dest.Y);

			if (ticks >= beamTicks)
				yield break;

			Color rc = Color.FromArgb((beamTicks-ticks)*255/beamTicks, color);
			
			float2 unit = 1.0f/(args.src - args.dest).Length*(args.src - args.dest).ToFloat2();
			float2 norm = new float2(-unit.Y, unit.X);
			
			var wlr = Game.Renderer.WorldLineRenderer;
			for (int i = -radius; i < radius; i++)
				wlr.DrawLine(args.src + i * norm, args.dest + i * norm, rc, rc);
		}
	}
}
