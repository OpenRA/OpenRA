#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Support;
using System.Collections.Generic;

namespace OpenRA.Traits
{
	public static class Util
	{
		public static int TickFacing( int facing, int desiredFacing, int rot )
		{
			var leftTurn = ( facing - desiredFacing ) & 0xFF;
			var rightTurn = ( desiredFacing - facing ) & 0xFF;
			if( Math.Min( leftTurn, rightTurn ) < rot )
				return desiredFacing & 0xFF;
			else if( rightTurn < leftTurn )
				return ( facing + rot ) & 0xFF;
			else
				return ( facing - rot ) & 0xFF;
		}

		public static int GetFacing( int2 d, int currentFacing )
		{
			if (d == int2.Zero)
				return currentFacing;

			int highest = -1;
			int highestDot = -1;

			for( int i = 0 ; i < fvecs.Length ; i++ )
			{
				int dot = int2.Dot( fvecs[ i ], d );
				if( dot > highestDot )
				{
					highestDot = dot;
					highest = i;
				}
			}

			return highest * 8;
		}

		public static int GetNearestFacing( int facing, int desiredFacing )
		{
			var turn = desiredFacing - facing;
			if( turn > 128 )
				turn -= 256;
			if( turn < -128 )
				turn += 256;

			return facing + turn;
		}

		public static int QuantizeFacing(int facing, int numFrames)
		{
			var step = 256 / numFrames;
			var a = (facing + step / 2) & 0xff;
			return a / step;
		}

		public static float2 RotateVectorByFacing(float2 v, int facing, float ecc)
		{
			var angle = (facing / 256f) * (2 * (float)Math.PI);
			var sinAngle = (float)Math.Sin(angle);
			var cosAngle = (float)Math.Cos(angle);

			return new float2(
				(cosAngle * v.X + sinAngle * v.Y),
				ecc * (cosAngle * v.Y - sinAngle * v.X));
		}

		public static int2 CenterOfCell(int2 loc)
		{
			return new int2( Game.CellSize / 2, Game.CellSize / 2 ) + Game.CellSize * loc;
		}

		public static int2 BetweenCells(int2 from, int2 to)
		{
			return int2.Lerp( CenterOfCell( from ), CenterOfCell( to ), 1, 2 );
		}

		public static int2 AsInt2(this int[] xs) { return new int2(xs[0], xs[1]); }
		public static float2 RelOffset(this int[] offset) { return new float2(offset[0], offset[1]); }
		public static float2 AbsOffset(this int[] offset) { return new float2(offset.ElementAtOrDefault(2), offset.ElementAtOrDefault(3)); }

		public static Renderable Centered(Actor self, Sprite s, float2 location)
		{
			var pal = self.Owner == null ? "player0" : self.Owner.Palette;
            var loc = location - 0.5f * s.size;

			return new Renderable(s, loc.Round(), pal, (int)self.CenterLocation.Y);
		}

		public static IActivity SequenceActivities(params IActivity[] acts)
		{
			return acts.Reverse().Aggregate(
				(next, a) => { a.Queue( next ); return a; });
		}

		public static IActivity RunActivity( Actor self, IActivity act )
		{
			while( act != null )
			{
				var prev = act;

				var sw = new Stopwatch();
				act = act.Tick( self );
				var dt = sw.ElapsedTime();
				if(dt > Game.Settings.Debug.LongTickThreshold)
					Log.Write("perf", "[{2}] Activity: {0} ({1:0.000} ms)", prev, dt * 1000, Game.LocalTick);

				if( prev == act )
					break;
			}
			return act;
		}

		public static Color ArrayToColor(int[] x) { return Color.FromArgb(x[0], x[1], x[2]); }

		public static int2 CellContaining(float2 pos) { return (1f / Game.CellSize * pos).ToInt2(); }

        /* pretty crap */
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> ts, Thirdparty.Random random)
        {
            var items = ts.ToList();
            while (items.Count > 0)
            {
                var t = items.Random(random);
                yield return t;
                items.Remove(t);
            }
        }
		
		static int2[] fvecs =
		{		
			new int2( 0, -1331 ),
			new int2( -199, -1305 ),
			new int2( -391, -1229 ),
			new int2( -568, -1106 ),
			new int2( -724, -941 ),
			new int2( -851, -739 ),
			new int2( -946, -509 ),
			new int2( -1004, -259 ),
			new int2( -1024, 0 ),
			new int2( -1004, 259 ),
			new int2( -946, 509 ),
			new int2( -851, 739 ),
			new int2( -724, 941 ),
			new int2( -568, 1106 ),
			new int2( -391, 1229 ),
			new int2( -199, 1305 ),
			new int2( 0, 1331 ),
			new int2( 199, 1305 ),
			new int2( 391, 1229 ),
			new int2( 568, 1106 ),
			new int2( 724, 941 ),
			new int2( 851, 739 ),
			new int2( 946, 509 ),
			new int2( 1004, 259 ),
			new int2( 1024, 0 ),
			new int2( 1004, -259 ),
			new int2( 946, -509 ),
			new int2( 851, -739 ),
			new int2( 724, -941 ),
			new int2( 568, -1106 ),
			new int2( 391, -1229 ),
			new int2( 199, -1305 )
		};
		}
}
