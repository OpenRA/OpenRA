#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.Activities;
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

		public static WPos BetweenCells(World w, CPos from, CPos to)
		{
			return WPos.Lerp(w.Map.CenterOfCell(from), w.Map.CenterOfCell(to), 1, 2);
		}

		public static Activity SequenceActivities(params Activity[] acts)
		{
			return acts.Reverse().Aggregate(
				(next, a) => { a.Queue(next); return a; });
		}

		public static Activity RunActivity(Actor self, Activity act)
		{
			// Note - manual iteration here for performance due to high call volume.
			var longTickThresholdInStopwatchTicks = PerfTimer.LongTickThresholdInStopwatchTicks;
			var start = Stopwatch.GetTimestamp();
			while (act != null)
			{
				var prev = act;
				act = act.Tick(self);
				var current = Stopwatch.GetTimestamp();
				if (current - start > longTickThresholdInStopwatchTicks)
				{
					PerfTimer.LogLongTick(start, current, "Activity", prev);
					start = Stopwatch.GetTimestamp();
				}
				else
					start = current;

				if (prev == act)
					break;
			}

			return act;
		}

		/* pretty crap */
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> ts, MersenneTwister random)
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
			return cells.SelectMany(c => Neighbours(c, allowDiagonal)).Distinct();
		}

		public static IEnumerable<CPos> AdjacentCells(World w, Target target)
		{
			var cells = target.Positions.Select(p => w.Map.CellContaining(p)).Distinct();
			return ExpandFootprint(cells, true);
		}

		public static int ApplyPercentageModifiers(int number, IEnumerable<int> percentages)
		{
			// See the comments of PR#6079 for a faster algorithm if this becomes a performance bottleneck
			var a = (decimal)number;
			foreach (var p in percentages)
				a *= p / 100m;

			return (int)a;
		}

		// Algorithm obtained from ftp://ftp.isc.org/pub/usenet/comp.sources.unix/volume26/line3d
		public static IEnumerable<CPos> Raycast(CPos source, CPos target)
		{
			var xDelta = target.X - source.X;
			var yDelta = target.Y - source.Y;

			var xAbsolute = Math.Abs(xDelta) << 1;
			var yAbsolute = Math.Abs(yDelta) << 1;

			var xIncrement = (xDelta < 0) ? -1 : xDelta > 0 ? 1 : 0;
			var yIncrement = (yDelta < 0) ? -1 : yDelta > 0 ? 1 : 0;

			var x = source.X;
			var y = source.Y;

			if (xAbsolute >= yAbsolute)
			{
				var error = yAbsolute - (xAbsolute >> 1);

				do
				{
					yield return new CPos(x, y);

					if (error >= 0)
					{
						y += yIncrement;
						error -= xAbsolute;
					}

					x += xIncrement;
					error += yAbsolute;
				}
				while (y != target.Y);
				yield return new CPos(x, y);
			}
			else
			{
				var error = xAbsolute - (yAbsolute >> 1);
				do
				{
					yield return new CPos(x, y);

					if (error >= 0)
					{
						x += xIncrement;
						error -= yAbsolute;
					}

					y += yIncrement;
					error += xAbsolute;
				}
				while (y != target.Y);
				yield return new CPos(x, y);
			}
		}

		public static int2? IntersectionPoint(int2 firstStart, int2 firstEnd, int2 secondStart, int2 secondEnd)
		{
			int denominator = (firstStart.X - firstEnd.X) * (secondStart.Y - secondEnd.Y) - (firstStart.Y - firstEnd.Y) * (secondStart.X - secondEnd.X);
			if (denominator == 0)
				return null;

			int firstCross = firstStart.X * firstEnd.Y - firstStart.Y * firstEnd.X;
			int secondCross = secondStart.X * secondEnd.Y - secondStart.Y * secondEnd.X;

			var possible = new int2(firstCross * (secondStart.X - secondEnd.X) - (firstStart.X - firstEnd.X) * secondCross / denominator,
				firstCross * (secondStart.Y - secondEnd.Y) - (firstStart.Y - firstEnd.Y) * secondCross / denominator);

			if (firstStart.X >= firstEnd.X)
			{
				if (possible.X > firstStart.X && possible.X < firstEnd.X)
					return null;
			}
			else
			{
				if (possible.X < firstStart.X && possible.X > firstEnd.X)
					return null;
			}

			if (firstStart.Y >= firstEnd.Y)
			{
				if (possible.Y > firstStart.Y && possible.Y < firstEnd.Y)
					return null;
			}
			else
			{
				if (possible.Y < firstStart.Y && possible.Y > firstEnd.Y)
					return null;
			}

			if (secondStart.X >= secondEnd.X)
			{
				if (possible.X > secondStart.X && possible.X < secondEnd.X)
					return null;
			}
			else
			{
				if (possible.X < secondStart.X && possible.X > secondEnd.X)
					return null;
			}

			if (secondStart.Y >= secondEnd.Y)
			{
				if (possible.Y > secondStart.Y && possible.Y < secondEnd.Y)
					return null;
			}
			else
			{
				if (possible.Y < secondStart.Y && possible.Y > secondEnd.Y)
					return null;
			}

			return possible;
		}

		public static IEnumerable<int2> LineCast(int2 start, int2 end, int2 mapSize)
		{
			var lineStart = start;
			var lineEnd = end;

			if (start.X > end.X)
			{
				lineStart = end;
				lineEnd = start;
			}

			var xDiff = lineEnd.X - (lineStart.X - 1);
			var yDiff = lineEnd.Y - (lineStart.Y - 1);
			var greater = xDiff >= yDiff;

			if (Math.Sign(xDiff) <= 0 || Math.Sign(yDiff) <= 0)
				throw new InvalidOperationException("Distance cannot be imaginary or zero.");

			for (var i = 0; (greater ? xDiff : yDiff) > i; i++)
			{
				var w = (greater ? lineStart.X : lineStart.Y) + i;
				var secondlineStart = new int2(greater ? w : 0, greater ? 0 : w);
				var secondlineEnd = new int2(greater ? w : mapSize.X, greater ? mapSize.Y : w);

				var result = IntersectionPoint(lineStart, lineEnd, secondlineStart, secondlineEnd);

				if (result.HasValue)
				{
					yield return result.Value;
				}
			}
		}
	}
}
