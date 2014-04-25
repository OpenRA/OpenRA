#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;

namespace OpenRA.Traits
{
	public static class Util
	{
		public static int TickFacing(int facing, int desiredFacing, int rot)
		{
			var leftTurn = (facing - desiredFacing) & 0xFF;
			var rightTurn = (desiredFacing - facing) & 0xFF;
			if (Math.Min(leftTurn, rightTurn) < rot)
				return desiredFacing & 0xFF;
			else if (rightTurn < leftTurn)
				return (facing + rot) & 0xFF;
			else
				return (facing - rot) & 0xFF;
		}

		public static int GetFacing(WVec d, int currentFacing)
		{
			if (d.LengthSquared == 0)
				return currentFacing;

			// OpenRA defines north as -y, so invert
			var angle = WAngle.ArcTan(-d.Y, d.X, 4).Angle;

			// Convert back to a facing
			return (angle / 4 - 0x40) & 0xFF;
		}

		public static int GetFacing(CVec d, int currentFacing)
		{
			return GetFacing(d.ToWVec(), currentFacing);
		}

		public static int GetNearestFacing(int facing, int desiredFacing)
		{
			var turn = desiredFacing - facing;
			if (turn > 128)
				turn -= 256;
			if (turn < -128)
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
				(next, a) => { a.Queue(next); return a; });
		}

		public static Activity RunActivity(Actor self, Activity act)
		{
			while (act != null)
			{
				var prev = act;

				var sw = new Stopwatch();
				act = act.Tick(self);
				var dt = sw.Elapsed;
				if (dt > Game.Settings.Debug.LongTickThreshold)
					Log.Write("perf", "[{2}] Activity: {0} ({1:0.000} ms)", prev, dt.TotalMilliseconds, Game.LocalTick);

				if (prev == act)
					break;
			}

			return act;
		}

		/* pretty crap */
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> ts, Support.Random random)
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
			var cells = target.Positions.Select(p => p.ToCPos()).Distinct();
			return ExpandFootprint(cells, true);
		}
	}
}
