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

		static float2[] fvecs = Graphics.Util.MakeArray<float2>( 32,
			i => -float2.FromAngle( i / 16.0f * (float)Math.PI ) * new float2( 1f, 1.3f ) );

		public static int _GetFacing( float2 d, int currentFacing )
		{
			if (float2.WithinEpsilon(d, float2.Zero, 0.001f))
				return currentFacing;

			int highest = -1;
			float highestDot = -1.0f;

			for( int i = 0 ; i < fvecs.Length ; i++ )
			{
				float dot = float2.Dot( fvecs[ i ], d );
				if( dot > highestDot )
				{
					highestDot = dot;
					highest = i;
				}
			}

			return highest * 8;
		}

		public static int GetFacing(float2 d, int currentFacing)
		{
			var result = _GetFacing(d, currentFacing);
			Log.Write("debug", "GetFacing {0} {1} => {2}", d, currentFacing, result);
			return result;
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

		public static float2 CenterOfCell(int2 loc)
		{
			return new float2(12, 12) + Game.CellSize * (float2)loc;
		}

		public static float2 BetweenCells(int2 from, int2 to)
		{
			return 0.5f * (CenterOfCell(from) + CenterOfCell(to));
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

		public static Color ArrayToColor(int[] x) { return Color.FromArgb(x[0], x[1], x[2]); }

		public static int2 CellContaining(float2 pos) { return (1f / Game.CellSize * pos).ToInt2(); }
	}
}
