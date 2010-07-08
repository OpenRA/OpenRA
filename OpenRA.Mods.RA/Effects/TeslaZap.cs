#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class TeslaZapInfo : IProjectileInfo
	{
		public IEffect Create(ProjectileArgs args) { return new TeslaZap( this, args ); }
	}

	class TeslaZap : IEffect
	{
		readonly ProjectileArgs Args;
		int timeUntilRemove = 2; // # of frames
		bool doneDamage = false;

		const int numZaps = 3;

		readonly List<Renderable> renderables = new List<Renderable>();

		public TeslaZap(TeslaZapInfo info, ProjectileArgs args)
		{
			Args = args;
			var bright = SequenceProvider.GetSequence("litning", "bright");
			var dim = SequenceProvider.GetSequence("litning", "dim");

			for (var n = 0; n < numZaps; n++)
				renderables.AddRange(DrawZapWandering(args.src, args.dest, n == numZaps - 1 ? bright : dim));
		}

		public void Tick( World world )
		{
			if( timeUntilRemove <= 0 )
				world.AddFrameEndTask( w => w.Remove( this ) );
			--timeUntilRemove;

			if (!doneDamage)
			{
				Combat.DoImpacts(Args);
				doneDamage = true;
			}
		}

		public IEnumerable<Renderable> Render() { return renderables; }

		static IEnumerable<Renderable> DrawZapWandering(int2 from, int2 to, Sequence s)
		{
			var z = float2.Zero;	/* hack */
			var dist = to - from;
			var norm = (1f / dist.Length) * new float2(-dist.Y, dist.X);

			var renderables = new List<Renderable>();
			if (Game.CosmeticRandom.Next(2) != 0)
			{
				var p1 = from + (1 / 3f) * dist.ToFloat2() + Game.CosmeticRandom.Gauss1D(1) * .2f * dist.Length * norm;
				var p2 = from + (2 / 3f) * dist.ToFloat2() + Game.CosmeticRandom.Gauss1D(1) * .2f * dist.Length * norm;

				renderables.AddRange(DrawZap(from, p1, s, out p1));
				renderables.AddRange(DrawZap(p1, p2, s, out p2));
				renderables.AddRange(DrawZap(p2, to, s, out z));
			}
			else
			{
				var p1 = from + (1 / 2f) * dist.ToFloat2() + Game.CosmeticRandom.Gauss1D(1) * .2f * dist.Length * norm;

				renderables.AddRange(DrawZap(from, p1, s, out p1));
				renderables.AddRange(DrawZap(p1, to, s, out z));
			}

			return renderables;
		}

		static IEnumerable<Renderable> DrawZap(float2 from, float2 to, Sequence s, out float2 p)
		{
			var dist = to - from;
			var q = new float2(-dist.Y, dist.X);
			var c = -float2.Dot(from, q);
			var rs = new List<Renderable>();
			var z = from;

			while ((to - z).X > 5 || (to - z).X < -5 || (to - z).Y > 5 || (to - z).Y < -5)
			{
				var step = steps.Where(t => (to - (z + new float2(t[0],t[1]))).LengthSquared < (to - z).LengthSquared )
					.OrderBy(t => Math.Abs(float2.Dot(z + new float2(t[0], t[1]), q) + c)).First();

				rs.Add(new Renderable(s.GetSprite(step[4]), z + new float2(step[2], step[3]), "effect"));
				z += new float2(step[0], step[1]);
			}

			p = z;

			return rs;
		}

		static int[][] steps = new [] 
		{ 
			new int[] { 8, 8, -8, -8, 0 },
			new int[] { -8, -8, -16, -16, 0 },
			new int[] { 8, 0, -8, -8, 1 },
			new int[] { -8, 0, -16, -8, 1 },
			new int[] { 0, 8, -8, -8, 2 },
			new int[] { 0, -8, -8, -16, 2 },
			new int[] { -8, 8, -16, -8, 3 },
			new int[] { 8, -8, -8, -16, 3 }
		};
	}
}
