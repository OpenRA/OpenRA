#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
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

		public static int QuantizeFacing(int facing, int numFrames, bool useClassicFacingFudge)
		{
			if (!useClassicFacingFudge || numFrames != 32)
				return Util.QuantizeFacing(facing, numFrames);

			// TD and RA divided the facing artwork into 3 frames from (north|south) to (north|south)-(east|west)
			// and then 5 frames from (north|south)-(east|west) to (east|west)
			var quadrant = ((facing + 31) & 0xFF) / 64;
			if (quadrant == 0 || quadrant == 2)
			{
				var frame = Util.QuantizeFacing(facing, 24);
				if (frame > 18)
					return frame + 6;
				if (frame > 4)
					return frame + 3;
				return frame;
			}
			else
			{
				var frame = Util.QuantizeFacing(facing, 40);
				return frame < 20 ? frame - 3 : frame - 8;
			}
		}

		/// <summary>Wraps an arbitrary integer facing value into the range 0 - 255</summary>
		public static int NormalizeFacing(int f)
		{
			if (f >= 0)
				return f & 0xFF;

			var negative = -f & 0xFF;
			return negative == 0 ? 0 : 256 - negative;
		}

		public static WPos BetweenCells(World w, CPos from, CPos to)
		{
			var fromPos = from.Layer == 0 ? w.Map.CenterOfCell(from) :
				w.GetCustomMovementLayers()[from.Layer].CenterOfCell(from);

			var toPos = to.Layer == 0 ? w.Map.CenterOfCell(to) :
				w.GetCustomMovementLayers()[to.Layer].CenterOfCell(to);

			return WPos.Lerp(fromPos, toPos, 1, 2);
		}

		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> ts, MersenneTwister random)
		{
			// Fisher-Yates
			var items = ts.ToArray();
			for (var i = 0; i < items.Length - 1; i++)
			{
				var j = random.Next(items.Length - i);
				var item = items[i + j];
				items[i + j] = items[i];
				items[i] = item;
				yield return item;
			}

			if (items.Length > 0)
				yield return items[items.Length - 1];
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

		public static IEnumerable<CPos> RandomWalk(CPos p, MersenneTwister r)
		{
			for (;;)
			{
				var dx = r.Next(-1, 2);
				var dy = r.Next(-1, 2);

				if (dx == 0 && dy == 0)
					continue;

				p += new CVec(dx, dy);
				yield return p;
			}
		}

		public static int RandomDelay(World world, int[] range)
		{
			if (range.Length == 0)
				return 0;

			if (range.Length == 1)
				return range[0];

			return world.SharedRandom.Next(range[0], range[1]);
		}

		// TODO: Investigate caching this or moving it to ActorMapInfo
		public static WDist MinimumRequiredVictimScanRadius(Ruleset rules)
		{
			return rules.Actors.SelectMany(a => a.Value.TraitInfos<HitShapeInfo>()).Max(h => h.Type.OuterRadius);
		}

		// TODO: Investigate caching this or moving it to ActorMapInfo
		public static WDist MinimumRequiredBlockerScanRadius(Ruleset rules)
		{
			return rules.Actors.Where(a => a.Value.HasTraitInfo<IBlocksProjectilesInfo>())
				.SelectMany(a => a.Value.TraitInfos<HitShapeInfo>()).Max(h => h.Type.OuterRadius);
		}

		public static string FriendlyTypeName(Type t)
		{
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>))
				return "Set of {0}".F(t.GetGenericArguments().Select(FriendlyTypeName).ToArray());

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
				return "Mapping of {0} to {1}".F(t.GetGenericArguments().Select(FriendlyTypeName).ToArray());

			if (t.IsSubclassOf(typeof(Array)))
				return "Multiple {0}".F(FriendlyTypeName(t.GetElementType()));

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Primitives.Cache<,>))
				return "Cached<{0},{1}>".F(t.GetGenericArguments().Select(FriendlyTypeName).ToArray());

			if (t == typeof(int) || t == typeof(uint))
				return "Integer";

			if (t == typeof(int2))
				return "2D Integer";

			if (t == typeof(float) || t == typeof(decimal))
				return "Real Number";

			if (t == typeof(float2))
				return "2D Real Number";

			if (t == typeof(CPos))
				return "2D Cell Position";

			if (t == typeof(CVec))
				return "2D Cell Vector";

			if (t == typeof(WAngle))
				return "1D World Angle";

			if (t == typeof(WRot))
				return "3D World Rotation";

			if (t == typeof(WPos))
				return "3D World Position";

			if (t == typeof(WDist))
				return "1D World Distance";

			if (t == typeof(WVec))
				return "3D World Vector";

			if (t == typeof(HSLColor) || t == typeof(Color))
				return "Color (RRGGBB[AA] notation)";

			return t.Name;
		}
	}
}
