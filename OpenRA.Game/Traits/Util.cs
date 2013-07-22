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

		public static int GetFacing(WVec d, int currentFacing)
		{
			return GetFacing(new int2(d.X, d.Y), currentFacing);
		}

		public static int GetFacing(PVecInt d, int currentFacing)
		{
			return GetFacing(d.ToInt2(), currentFacing);
		}

		public static int GetFacing(CVec d, int currentFacing)
		{
			return GetFacing(d.ToInt2(), currentFacing);
		}

		public static int GetFacing(int2 d, int currentFacing)
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

		public static WPos BetweenCells(CPos from, CPos to)
		{
			return WPos.Lerp(from.CenterPosition, to.CenterPosition, 1, 2);
		}

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
	}
}
