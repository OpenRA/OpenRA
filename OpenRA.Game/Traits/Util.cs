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

		public static int GetFacing(PVecInt d, int currentFacing)
		{
			return GetFacing(d.ToInt2(), currentFacing);
		}

		public static int GetFacing(CVec d, int currentFacing)
		{
			return GetFacing(d.ToInt2(), currentFacing);
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

		public static PPos CenterOfCell(CPos loc)
		{
			return loc.ToPPos() + new PVecInt(Game.CellSize / 2, Game.CellSize / 2);
		}

		public static PPos BetweenCells(CPos from, CPos to)
		{
			return PPos.Lerp(CenterOfCell(from), CenterOfCell(to), 1, 2);
		}

		public static int2 AsInt2(this int[] xs) { return new int2(xs[0], xs[1]); }
		public static float2 RelOffset(this int[] offset) { return new float2(offset[0], offset[1]); }
		public static float2 AbsOffset(this int[] offset) { return new float2(offset.ElementAtOrDefault(2), offset.ElementAtOrDefault(3)); }

		public static Activity SequenceActivities(params Activity[] acts)
		{
			return acts.Reverse().Aggregate(
				(next, a) => { a.Queue( next ); return a; });
		}

		public static Activity RunActivity(Actor self, Activity act)
		{
			while (act != null)
			{
				var prev = act;

				var sw = new Stopwatch();
				act = act.Tick(self);
				var dt = sw.ElapsedTime();
				if (dt > Game.Settings.Debug.LongTickThreshold)
					Log.Write("perf", "[{2}] Activity: {0} ({1:0.000} ms)", prev, dt * 1000, Game.LocalTick);

				if (prev == act)
					break;
			}
			return act;
		}

		public static Color ArrayToColor(int[] x) { return Color.FromArgb(x[0], x[1], x[2]); }

		[Obsolete("Use ToCPos() method", true)]
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

		static IEnumerable<CPos> Neighbours(CPos c, bool allowDiagonal)
		{
			yield return c;
			yield return new CPos(c.X - 1, c.Y);
			yield return new CPos(c.X + 1, c.Y);
			yield return new CPos(c.X, c.Y - 1);
			yield return new CPos(c.X, c.Y + 1);

			if (allowDiagonal)
			{
				yield return new CPos(c.X - 1, c.Y - 1);
				yield return new CPos(c.X + 1, c.Y - 1);
				yield return new CPos(c.X - 1, c.Y + 1);
				yield return new CPos(c.X + 1, c.Y + 1);
			}
		}

		public static IEnumerable<CPos> ExpandFootprint(IEnumerable<CPos> cells, bool allowDiagonal)
		{
			var result = new Dictionary<CPos, bool>();
			foreach (var c in cells.SelectMany(c => Neighbours(c, allowDiagonal)))
				result[c] = true;
			return result.Keys;
		}

		public static IEnumerable<CPos> AdjacentCells(Target target)
		{
			var cells = target.IsActor
				? target.Actor.OccupiesSpace.OccupiedCells().Select(c => c.First).ToArray()
				: new CPos[] {};

			if (cells.Length == 0)
				cells = new CPos[] { target.CenterPosition.ToCPos() };

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


		public static readonly PSubVec[] SubPxVector =
		{
			new PSubVec( 0, 1024 ),
			new PSubVec( 25, 1023 ),
			new PSubVec( 50, 1022 ),
			new PSubVec( 75, 1021 ),
			new PSubVec( 100, 1019 ),
			new PSubVec( 125, 1016 ),
			new PSubVec( 150, 1012 ),
			new PSubVec( 175, 1008 ),
			new PSubVec( 199, 1004 ),
			new PSubVec( 224, 999 ),
			new PSubVec( 248, 993 ),
			new PSubVec( 273, 986 ),
			new PSubVec( 297, 979 ),
			new PSubVec( 321, 972 ),
			new PSubVec( 344, 964 ),
			new PSubVec( 368, 955 ),
			new PSubVec( 391, 946 ),
			new PSubVec( 414, 936 ),
			new PSubVec( 437, 925 ),
			new PSubVec( 460, 914 ),
			new PSubVec( 482, 903 ),
			new PSubVec( 504, 890 ),
			new PSubVec( 526, 878 ),
			new PSubVec( 547, 865 ),
			new PSubVec( 568, 851 ),
			new PSubVec( 589, 837 ),
			new PSubVec( 609, 822 ),
			new PSubVec( 629, 807 ),
			new PSubVec( 649, 791 ),
			new PSubVec( 668, 775 ),
			new PSubVec( 687, 758 ),
			new PSubVec( 706, 741 ),
			new PSubVec( 724, 724 ),
			new PSubVec( 741, 706 ),
			new PSubVec( 758, 687 ),
			new PSubVec( 775, 668 ),
			new PSubVec( 791, 649 ),
			new PSubVec( 807, 629 ),
			new PSubVec( 822, 609 ),
			new PSubVec( 837, 589 ),
			new PSubVec( 851, 568 ),
			new PSubVec( 865, 547 ),
			new PSubVec( 878, 526 ),
			new PSubVec( 890, 504 ),
			new PSubVec( 903, 482 ),
			new PSubVec( 914, 460 ),
			new PSubVec( 925, 437 ),
			new PSubVec( 936, 414 ),
			new PSubVec( 946, 391 ),
			new PSubVec( 955, 368 ),
			new PSubVec( 964, 344 ),
			new PSubVec( 972, 321 ),
			new PSubVec( 979, 297 ),
			new PSubVec( 986, 273 ),
			new PSubVec( 993, 248 ),
			new PSubVec( 999, 224 ),
			new PSubVec( 1004, 199 ),
			new PSubVec( 1008, 175 ),
			new PSubVec( 1012, 150 ),
			new PSubVec( 1016, 125 ),
			new PSubVec( 1019, 100 ),
			new PSubVec( 1021, 75 ),
			new PSubVec( 1022, 50 ),
			new PSubVec( 1023, 25 ),
			new PSubVec( 1024, 0 ),
			new PSubVec( 1023, -25 ),
			new PSubVec( 1022, -50 ),
			new PSubVec( 1021, -75 ),
			new PSubVec( 1019, -100 ),
			new PSubVec( 1016, -125 ),
			new PSubVec( 1012, -150 ),
			new PSubVec( 1008, -175 ),
			new PSubVec( 1004, -199 ),
			new PSubVec( 999, -224 ),
			new PSubVec( 993, -248 ),
			new PSubVec( 986, -273 ),
			new PSubVec( 979, -297 ),
			new PSubVec( 972, -321 ),
			new PSubVec( 964, -344 ),
			new PSubVec( 955, -368 ),
			new PSubVec( 946, -391 ),
			new PSubVec( 936, -414 ),
			new PSubVec( 925, -437 ),
			new PSubVec( 914, -460 ),
			new PSubVec( 903, -482 ),
			new PSubVec( 890, -504 ),
			new PSubVec( 878, -526 ),
			new PSubVec( 865, -547 ),
			new PSubVec( 851, -568 ),
			new PSubVec( 837, -589 ),
			new PSubVec( 822, -609 ),
			new PSubVec( 807, -629 ),
			new PSubVec( 791, -649 ),
			new PSubVec( 775, -668 ),
			new PSubVec( 758, -687 ),
			new PSubVec( 741, -706 ),
			new PSubVec( 724, -724 ),
			new PSubVec( 706, -741 ),
			new PSubVec( 687, -758 ),
			new PSubVec( 668, -775 ),
			new PSubVec( 649, -791 ),
			new PSubVec( 629, -807 ),
			new PSubVec( 609, -822 ),
			new PSubVec( 589, -837 ),
			new PSubVec( 568, -851 ),
			new PSubVec( 547, -865 ),
			new PSubVec( 526, -878 ),
			new PSubVec( 504, -890 ),
			new PSubVec( 482, -903 ),
			new PSubVec( 460, -914 ),
			new PSubVec( 437, -925 ),
			new PSubVec( 414, -936 ),
			new PSubVec( 391, -946 ),
			new PSubVec( 368, -955 ),
			new PSubVec( 344, -964 ),
			new PSubVec( 321, -972 ),
			new PSubVec( 297, -979 ),
			new PSubVec( 273, -986 ),
			new PSubVec( 248, -993 ),
			new PSubVec( 224, -999 ),
			new PSubVec( 199, -1004 ),
			new PSubVec( 175, -1008 ),
			new PSubVec( 150, -1012 ),
			new PSubVec( 125, -1016 ),
			new PSubVec( 100, -1019 ),
			new PSubVec( 75, -1021 ),
			new PSubVec( 50, -1022 ),
			new PSubVec( 25, -1023 ),
			new PSubVec( 0, -1024 ),
			new PSubVec( -25, -1023 ),
			new PSubVec( -50, -1022 ),
			new PSubVec( -75, -1021 ),
			new PSubVec( -100, -1019 ),
			new PSubVec( -125, -1016 ),
			new PSubVec( -150, -1012 ),
			new PSubVec( -175, -1008 ),
			new PSubVec( -199, -1004 ),
			new PSubVec( -224, -999 ),
			new PSubVec( -248, -993 ),
			new PSubVec( -273, -986 ),
			new PSubVec( -297, -979 ),
			new PSubVec( -321, -972 ),
			new PSubVec( -344, -964 ),
			new PSubVec( -368, -955 ),
			new PSubVec( -391, -946 ),
			new PSubVec( -414, -936 ),
			new PSubVec( -437, -925 ),
			new PSubVec( -460, -914 ),
			new PSubVec( -482, -903 ),
			new PSubVec( -504, -890 ),
			new PSubVec( -526, -878 ),
			new PSubVec( -547, -865 ),
			new PSubVec( -568, -851 ),
			new PSubVec( -589, -837 ),
			new PSubVec( -609, -822 ),
			new PSubVec( -629, -807 ),
			new PSubVec( -649, -791 ),
			new PSubVec( -668, -775 ),
			new PSubVec( -687, -758 ),
			new PSubVec( -706, -741 ),
			new PSubVec( -724, -724 ),
			new PSubVec( -741, -706 ),
			new PSubVec( -758, -687 ),
			new PSubVec( -775, -668 ),
			new PSubVec( -791, -649 ),
			new PSubVec( -807, -629 ),
			new PSubVec( -822, -609 ),
			new PSubVec( -837, -589 ),
			new PSubVec( -851, -568 ),
			new PSubVec( -865, -547 ),
			new PSubVec( -878, -526 ),
			new PSubVec( -890, -504 ),
			new PSubVec( -903, -482 ),
			new PSubVec( -914, -460 ),
			new PSubVec( -925, -437 ),
			new PSubVec( -936, -414 ),
			new PSubVec( -946, -391 ),
			new PSubVec( -955, -368 ),
			new PSubVec( -964, -344 ),
			new PSubVec( -972, -321 ),
			new PSubVec( -979, -297 ),
			new PSubVec( -986, -273 ),
			new PSubVec( -993, -248 ),
			new PSubVec( -999, -224 ),
			new PSubVec( -1004, -199 ),
			new PSubVec( -1008, -175 ),
			new PSubVec( -1012, -150 ),
			new PSubVec( -1016, -125 ),
			new PSubVec( -1019, -100 ),
			new PSubVec( -1021, -75 ),
			new PSubVec( -1022, -50 ),
			new PSubVec( -1023, -25 ),
			new PSubVec( -1024, 0 ),
			new PSubVec( -1023, 25 ),
			new PSubVec( -1022, 50 ),
			new PSubVec( -1021, 75 ),
			new PSubVec( -1019, 100 ),
			new PSubVec( -1016, 125 ),
			new PSubVec( -1012, 150 ),
			new PSubVec( -1008, 175 ),
			new PSubVec( -1004, 199 ),
			new PSubVec( -999, 224 ),
			new PSubVec( -993, 248 ),
			new PSubVec( -986, 273 ),
			new PSubVec( -979, 297 ),
			new PSubVec( -972, 321 ),
			new PSubVec( -964, 344 ),
			new PSubVec( -955, 368 ),
			new PSubVec( -946, 391 ),
			new PSubVec( -936, 414 ),
			new PSubVec( -925, 437 ),
			new PSubVec( -914, 460 ),
			new PSubVec( -903, 482 ),
			new PSubVec( -890, 504 ),
			new PSubVec( -878, 526 ),
			new PSubVec( -865, 547 ),
			new PSubVec( -851, 568 ),
			new PSubVec( -837, 589 ),
			new PSubVec( -822, 609 ),
			new PSubVec( -807, 629 ),
			new PSubVec( -791, 649 ),
			new PSubVec( -775, 668 ),
			new PSubVec( -758, 687 ),
			new PSubVec( -741, 706 ),
			new PSubVec( -724, 724 ),
			new PSubVec( -706, 741 ),
			new PSubVec( -687, 758 ),
			new PSubVec( -668, 775 ),
			new PSubVec( -649, 791 ),
			new PSubVec( -629, 807 ),
			new PSubVec( -609, 822 ),
			new PSubVec( -589, 837 ),
			new PSubVec( -568, 851 ),
			new PSubVec( -547, 865 ),
			new PSubVec( -526, 878 ),
			new PSubVec( -504, 890 ),
			new PSubVec( -482, 903 ),
			new PSubVec( -460, 914 ),
			new PSubVec( -437, 925 ),
			new PSubVec( -414, 936 ),
			new PSubVec( -391, 946 ),
			new PSubVec( -368, 955 ),
			new PSubVec( -344, 964 ),
			new PSubVec( -321, 972 ),
			new PSubVec( -297, 979 ),
			new PSubVec( -273, 986 ),
			new PSubVec( -248, 993 ),
			new PSubVec( -224, 999 ),
			new PSubVec( -199, 1004 ),
			new PSubVec( -175, 1008 ),
			new PSubVec( -150, 1012 ),
			new PSubVec( -125, 1016 ),
			new PSubVec( -100, 1019 ),
			new PSubVec( -75, 1021 ),
			new PSubVec( -50, 1022 ),
			new PSubVec( -25, 1023 )
		};
	}
}
