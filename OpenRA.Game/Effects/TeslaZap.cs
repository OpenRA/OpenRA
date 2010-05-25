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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Effects
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
			var dist = to - from;
			var norm = (1f / dist.Length) * new float2(-dist.Y, dist.X);

			var renderables = new List<Renderable>();
			if (Game.CosmeticRandom.Next(2) != 0)
			{
				var p1 = from + (1 / 3f) * dist.ToFloat2() + Game.CosmeticRandom.Gauss1D(1) * .2f * dist.Length * norm;
				var p2 = from + (2 / 3f) * dist.ToFloat2() + Game.CosmeticRandom.Gauss1D(1) * .2f * dist.Length * norm;

				renderables.AddRange(DrawZap(from, p1.ToInt2(), s));
				renderables.AddRange(DrawZap(p1.ToInt2(), p2.ToInt2(), s));
				renderables.AddRange(DrawZap(p2.ToInt2(), to, s));
			}
			else
			{
				var p1 = from + (1 / 2f) * dist.ToFloat2() + Game.CosmeticRandom.Gauss1D(1) * .2f * dist.Length * norm;

				renderables.AddRange(DrawZap(from, p1.ToInt2(), s));
				renderables.AddRange(DrawZap(p1.ToInt2(), to, s));
			}

			return renderables;
		}

		static IEnumerable<Renderable> DrawZap(int2 from, int2 to, Sequence s)
		{
			if (from.X < to.X)
				return DrawZapInner(from, to, s);
			else if (from.X > to.X || from.Y > to.Y)
				return DrawZapInner(to, from, s);
			else
				return DrawZapInner(from, to, s);
		}

		static IEnumerable<Renderable> DrawZapInner( int2 from, int2 to, Sequence s )
		{
			int2 d = to - from;
			if( d.X < 8 )
			{
				var prev = new int2( 0, 0 );
				var y = d.Y;
				while( y >= prev.Y + 8 )
				{
					yield return new Renderable( s.GetSprite( 2 ), (float2)( from + prev - new int2( 0, 8 ) ), "effect");
					prev.Y += 8;
				}
			}
			else
			{
				var prev = new int2( 0, 0 );
				for( int i = 1 ; i < d.X ; i += 8 )
				{
					var y = i * d.Y / d.X;
					if( y <= prev.Y - 8 )
					{
						yield return new Renderable(s.GetSprite(3), (float2)(from + prev - new int2(8, 16)), "effect");
						prev.Y -= 8;
						while( y <= prev.Y - 8 )
						{
							yield return new Renderable(s.GetSprite(2), (float2)(from + prev - new int2(0, 16)), "effect");
							prev.Y -= 8;
						}
					}
					else if( y >= prev.Y + 8 )
					{
						yield return new Renderable(s.GetSprite(0), (float2)(from + prev - new int2(8, 8)), "effect");
						prev.Y += 8;
						while( y >= prev.Y + 8 )
						{
							yield return new Renderable(s.GetSprite(2), (float2)(from + prev - new int2(0, 8)), "effect");
							prev.Y += 8;
						}
					}
					else
						yield return new Renderable(s.GetSprite(1), (float2)(from + prev - new int2(8, 8)), "effect");

					prev.X += 8;
				}
			}
		}
	}
}
