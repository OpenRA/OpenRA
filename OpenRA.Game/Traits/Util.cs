#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Support;

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

		public static Activity SequenceActivities(params Activity[] acts)
		{
			return acts.Reverse().Aggregate(
				(next, a) => { a.Queue( next ); return a; });
		}

		public static Activity RunActivity( Actor self, Activity act )
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

		static IEnumerable<int2> Neighbours(int2 c, bool allowDiagonal)
		{
			yield return c;
			yield return new int2(c.X - 1, c.Y);
			yield return new int2(c.X + 1, c.Y);
			yield return new int2(c.X, c.Y - 1);
			yield return new int2(c.X, c.Y + 1);

			if (allowDiagonal)
			{
				yield return new int2(c.X - 1, c.Y - 1);
				yield return new int2(c.X + 1, c.Y - 1);
				yield return new int2(c.X - 1, c.Y + 1);
				yield return new int2(c.X + 1, c.Y + 1);
			}
		}

		public static IEnumerable<int2> ExpandFootprint(IEnumerable<int2> cells, bool allowDiagonal)
		{
			var result = new Dictionary<int2, bool>();
			foreach (var c in cells.SelectMany(c => Neighbours(c, allowDiagonal)))
				result[c] = true;
			return result.Keys;
		}

		public static IEnumerable<int2> AdjacentCells( Target target )
		{
			var cells = target.IsActor
				? target.Actor.OccupiesSpace.OccupiedCells().Select(c => c.First).ToArray()
				: new int2[] {};

			if (cells.Length == 0)
				cells = new [] { Util.CellContaining(target.CenterLocation) };

			return Util.ExpandFootprint(cells, true);
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


		public static readonly int2[] SubPxVector =
		{
			new int2( 0, 1024 ),
			new int2( 25, 1023 ),
			new int2( 50, 1022 ),
			new int2( 75, 1021 ),
			new int2( 100, 1019 ),
			new int2( 125, 1016 ),
			new int2( 150, 1012 ),
			new int2( 175, 1008 ),
			new int2( 199, 1004 ),
			new int2( 224, 999 ),
			new int2( 248, 993 ),
			new int2( 273, 986 ),
			new int2( 297, 979 ),
			new int2( 321, 972 ),
			new int2( 344, 964 ),
			new int2( 368, 955 ),
			new int2( 391, 946 ),
			new int2( 414, 936 ),
			new int2( 437, 925 ),
			new int2( 460, 914 ),
			new int2( 482, 903 ),
			new int2( 504, 890 ),
			new int2( 526, 878 ),
			new int2( 547, 865 ),
			new int2( 568, 851 ),
			new int2( 589, 837 ),
			new int2( 609, 822 ),
			new int2( 629, 807 ),
			new int2( 649, 791 ),
			new int2( 668, 775 ),
			new int2( 687, 758 ),
			new int2( 706, 741 ),
			new int2( 724, 724 ),
			new int2( 741, 706 ),
			new int2( 758, 687 ),
			new int2( 775, 668 ),
			new int2( 791, 649 ),
			new int2( 807, 629 ),
			new int2( 822, 609 ),
			new int2( 837, 589 ),
			new int2( 851, 568 ),
			new int2( 865, 547 ),
			new int2( 878, 526 ),
			new int2( 890, 504 ),
			new int2( 903, 482 ),
			new int2( 914, 460 ),
			new int2( 925, 437 ),
			new int2( 936, 414 ),
			new int2( 946, 391 ),
			new int2( 955, 368 ),
			new int2( 964, 344 ),
			new int2( 972, 321 ),
			new int2( 979, 297 ),
			new int2( 986, 273 ),
			new int2( 993, 248 ),
			new int2( 999, 224 ),
			new int2( 1004, 199 ),
			new int2( 1008, 175 ),
			new int2( 1012, 150 ),
			new int2( 1016, 125 ),
			new int2( 1019, 100 ),
			new int2( 1021, 75 ),
			new int2( 1022, 50 ),
			new int2( 1023, 25 ),
			new int2( 1024, 0 ),
			new int2( 1023, -25 ),
			new int2( 1022, -50 ),
			new int2( 1021, -75 ),
			new int2( 1019, -100 ),
			new int2( 1016, -125 ),
			new int2( 1012, -150 ),
			new int2( 1008, -175 ),
			new int2( 1004, -199 ),
			new int2( 999, -224 ),
			new int2( 993, -248 ),
			new int2( 986, -273 ),
			new int2( 979, -297 ),
			new int2( 972, -321 ),
			new int2( 964, -344 ),
			new int2( 955, -368 ),
			new int2( 946, -391 ),
			new int2( 936, -414 ),
			new int2( 925, -437 ),
			new int2( 914, -460 ),
			new int2( 903, -482 ),
			new int2( 890, -504 ),
			new int2( 878, -526 ),
			new int2( 865, -547 ),
			new int2( 851, -568 ),
			new int2( 837, -589 ),
			new int2( 822, -609 ),
			new int2( 807, -629 ),
			new int2( 791, -649 ),
			new int2( 775, -668 ),
			new int2( 758, -687 ),
			new int2( 741, -706 ),
			new int2( 724, -724 ),
			new int2( 706, -741 ),
			new int2( 687, -758 ),
			new int2( 668, -775 ),
			new int2( 649, -791 ),
			new int2( 629, -807 ),
			new int2( 609, -822 ),
			new int2( 589, -837 ),
			new int2( 568, -851 ),
			new int2( 547, -865 ),
			new int2( 526, -878 ),
			new int2( 504, -890 ),
			new int2( 482, -903 ),
			new int2( 460, -914 ),
			new int2( 437, -925 ),
			new int2( 414, -936 ),
			new int2( 391, -946 ),
			new int2( 368, -955 ),
			new int2( 344, -964 ),
			new int2( 321, -972 ),
			new int2( 297, -979 ),
			new int2( 273, -986 ),
			new int2( 248, -993 ),
			new int2( 224, -999 ),
			new int2( 199, -1004 ),
			new int2( 175, -1008 ),
			new int2( 150, -1012 ),
			new int2( 125, -1016 ),
			new int2( 100, -1019 ),
			new int2( 75, -1021 ),
			new int2( 50, -1022 ),
			new int2( 25, -1023 ),
			new int2( 0, -1024 ),
			new int2( -25, -1023 ),
			new int2( -50, -1022 ),
			new int2( -75, -1021 ),
			new int2( -100, -1019 ),
			new int2( -125, -1016 ),
			new int2( -150, -1012 ),
			new int2( -175, -1008 ),
			new int2( -199, -1004 ),
			new int2( -224, -999 ),
			new int2( -248, -993 ),
			new int2( -273, -986 ),
			new int2( -297, -979 ),
			new int2( -321, -972 ),
			new int2( -344, -964 ),
			new int2( -368, -955 ),
			new int2( -391, -946 ),
			new int2( -414, -936 ),
			new int2( -437, -925 ),
			new int2( -460, -914 ),
			new int2( -482, -903 ),
			new int2( -504, -890 ),
			new int2( -526, -878 ),
			new int2( -547, -865 ),
			new int2( -568, -851 ),
			new int2( -589, -837 ),
			new int2( -609, -822 ),
			new int2( -629, -807 ),
			new int2( -649, -791 ),
			new int2( -668, -775 ),
			new int2( -687, -758 ),
			new int2( -706, -741 ),
			new int2( -724, -724 ),
			new int2( -741, -706 ),
			new int2( -758, -687 ),
			new int2( -775, -668 ),
			new int2( -791, -649 ),
			new int2( -807, -629 ),
			new int2( -822, -609 ),
			new int2( -837, -589 ),
			new int2( -851, -568 ),
			new int2( -865, -547 ),
			new int2( -878, -526 ),
			new int2( -890, -504 ),
			new int2( -903, -482 ),
			new int2( -914, -460 ),
			new int2( -925, -437 ),
			new int2( -936, -414 ),
			new int2( -946, -391 ),
			new int2( -955, -368 ),
			new int2( -964, -344 ),
			new int2( -972, -321 ),
			new int2( -979, -297 ),
			new int2( -986, -273 ),
			new int2( -993, -248 ),
			new int2( -999, -224 ),
			new int2( -1004, -199 ),
			new int2( -1008, -175 ),
			new int2( -1012, -150 ),
			new int2( -1016, -125 ),
			new int2( -1019, -100 ),
			new int2( -1021, -75 ),
			new int2( -1022, -50 ),
			new int2( -1023, -25 ),
			new int2( -1024, 0 ),
			new int2( -1023, 25 ),
			new int2( -1022, 50 ),
			new int2( -1021, 75 ),
			new int2( -1019, 100 ),
			new int2( -1016, 125 ),
			new int2( -1012, 150 ),
			new int2( -1008, 175 ),
			new int2( -1004, 199 ),
			new int2( -999, 224 ),
			new int2( -993, 248 ),
			new int2( -986, 273 ),
			new int2( -979, 297 ),
			new int2( -972, 321 ),
			new int2( -964, 344 ),
			new int2( -955, 368 ),
			new int2( -946, 391 ),
			new int2( -936, 414 ),
			new int2( -925, 437 ),
			new int2( -914, 460 ),
			new int2( -903, 482 ),
			new int2( -890, 504 ),
			new int2( -878, 526 ),
			new int2( -865, 547 ),
			new int2( -851, 568 ),
			new int2( -837, 589 ),
			new int2( -822, 609 ),
			new int2( -807, 629 ),
			new int2( -791, 649 ),
			new int2( -775, 668 ),
			new int2( -758, 687 ),
			new int2( -741, 706 ),
			new int2( -724, 724 ),
			new int2( -706, 741 ),
			new int2( -687, 758 ),
			new int2( -668, 775 ),
			new int2( -649, 791 ),
			new int2( -629, 807 ),
			new int2( -609, 822 ),
			new int2( -589, 837 ),
			new int2( -568, 851 ),
			new int2( -547, 865 ),
			new int2( -526, 878 ),
			new int2( -504, 890 ),
			new int2( -482, 903 ),
			new int2( -460, 914 ),
			new int2( -437, 925 ),
			new int2( -414, 936 ),
			new int2( -391, 946 ),
			new int2( -368, 955 ),
			new int2( -344, 964 ),
			new int2( -321, 972 ),
			new int2( -297, 979 ),
			new int2( -273, 986 ),
			new int2( -248, 993 ),
			new int2( -224, 999 ),
			new int2( -199, 1004 ),
			new int2( -175, 1008 ),
			new int2( -150, 1012 ),
			new int2( -125, 1016 ),
			new int2( -100, 1019 ),
			new int2( -75, 1021 ),
			new int2( -50, 1022 ),
			new int2( -25, 1023 )
		};
	}
}
