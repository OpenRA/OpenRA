#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class LaserZapInfo : IProjectileInfo
	{
		public readonly int BeamRadius = 1;
		public readonly bool UsePlayerColor = false;

		public IEffect Create(ProjectileArgs args) 
		{
			Color c = UsePlayerColor ? args.firedBy.Owner.Color : Color.Red;
			return new LaserZap(args, BeamRadius, c);
		}
	}

	class LaserZap : IEffect
	{
		ProjectileArgs args;
		readonly int radius;
		int timeUntilRemove = 10; // # of frames
		int totalTime = 10;
		Color color;
		bool doneDamage = false;
		
		public LaserZap(ProjectileArgs args, int radius, Color color)
		{
			this.args = args;
			this.color = color;
			this.radius = radius;
		}

		public void Tick(World world)
		{
			if (timeUntilRemove <= 0)
				world.AddFrameEndTask(w => w.Remove(this));
			--timeUntilRemove;
			
			if (!doneDamage)
			{
				Combat.DoImpacts(args);
				doneDamage = true;
			}
		}

		public IEnumerable<Renderable> Render()
		{
			int alpha = (int)((1-(float)(totalTime-timeUntilRemove)/totalTime)*255);
			Color rc = Color.FromArgb(alpha,color);
			
			float2 unit = 1.0f/(args.src - args.dest).Length*(args.src - args.dest).ToFloat2();
			float2 norm = new float2(-unit.Y, unit.X);
			
			for (int i = -radius; i < radius; i++)
				Game.world.WorldRenderer.DrawLine(args.src + i * norm, args.dest + i * norm, rc, rc);
			
			yield break;
		}
	}
}
