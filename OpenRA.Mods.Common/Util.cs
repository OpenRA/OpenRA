#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public enum InaccuracyType { Maximum, PerCellIncrement, Absolute }

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

		/// <summary>
		/// Adds step angle units to facing in the direction that takes it closer to desiredFacing.
		/// If facing is already within step of desiredFacing then desiredFacing is returned.
		/// Step is given as an integer to allow negative values (step away from the desired facing).
		/// </summary>
		public static WAngle TickFacing(WAngle facing, WAngle desiredFacing, WAngle step)
		{
			var leftTurn = (facing - desiredFacing).Angle;
			var rightTurn = (desiredFacing - facing).Angle;
			if (leftTurn < step.Angle || rightTurn < step.Angle)
				return desiredFacing;

			return rightTurn < leftTurn ? facing + step : facing - step;
		}

		/// <summary>
		/// Determines whether desiredFacing is clockwise (-1) or anticlockwise (+1) of facing.
		/// If desiredFacing is equal to facing or directly behind facing we treat it as being anticlockwise.
		/// </summary>
		public static int GetTurnDirection(WAngle facing, WAngle desiredFacing)
		{
			return (facing - desiredFacing).Angle < 512 ? -1 : 1;
		}

		/// <summary>
		/// Calculate the frame index (between 0..numFrames) that
		/// should be used for the given facing value.
		/// </summary>
		public static int IndexFacing(WAngle facing, int numFrames)
		{
			// 1024 here is the max angle, so we divide the max angle by the total number of facings (numFrames)
			var step = 1024 / numFrames;
			var a = (facing.Angle + step / 2) & 1023;
			return a / step;
		}

		/// <summary>
		/// Returns the remainder angle after rounding to the nearest whole step / facing.
		/// </summary>
		public static WAngle AngleDiffToStep(WAngle facing, int numFrames)
		{
			var step = 1024 / numFrames;
			var a = (facing.Angle + step / 2) & 1023;
			return new WAngle(a % step - step / 2);
		}

		/// <summary>Returns the angle that the closest facing sprite should be rotated by to achieve the closest interpolated facing.</summary>
		public static WAngle GetInterpolatedFacingRotation(WAngle facing, int facings, int interpolatedFacings)
		{
			var step = 1024 / interpolatedFacings;
			return new WAngle(AngleDiffToStep(facing, facings).Angle / step * step);
		}

		/// <summary>Rounds the given facing value to the nearest quantized facing.</summary>
		public static WAngle QuantizeFacing(WAngle facing, int facings)
		{
			return new WAngle(IndexFacing(facing, facings) * (1024 / facings));
		}

		/// <summary>Wraps an arbitrary integer facing value into the range 0 - 255.</summary>
		public static int NormalizeFacing(int f)
		{
			if (f >= 0)
				return f & 0xFF;

			var negative = -f & 0xFF;
			return negative == 0 ? 0 : 256 - negative;
		}

		public static bool FacingWithinTolerance(WAngle facing, WAngle desiredFacing, WAngle facingTolerance)
		{
			if (facingTolerance.Angle == 0 && facing == desiredFacing)
				return true;

			var delta = (desiredFacing - facing).Angle;
			return delta <= facingTolerance.Angle || delta >= 1024 - facingTolerance.Angle;
		}

		public static WPos BetweenCells(World w, CPos from, CPos to)
		{
			var fromPos = from.Layer == 0 ? w.Map.CenterOfCell(from) :
				w.GetCustomMovementLayers()[from.Layer].CenterOfCell(from);

			var toPos = to.Layer == 0 ? w.Map.CenterOfCell(to) :
				w.GetCustomMovementLayers()[to.Layer].CenterOfCell(to);

			return WPos.Lerp(fromPos, toPos, 1, 2);
		}

		public static WAngle GetVerticalAngle(WPos source, WPos target)
		{
			var delta = target - source;
			var horizontalDelta = delta.HorizontalLength;
			var verticalVector = new WVec(-delta.Z, -horizontalDelta, 0);

			return verticalVector.Yaw;
		}

		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> ts, MersenneTwister random)
		{
			// Fisher-Yates
			var items = ts.ToArray();
			for (var i = 0; i < items.Length - 1; i++)
			{
				var j = random.Next(items.Length - i);
				(items[i], items[i + j]) = (items[i + j], items[i]);
				yield return items[i];
			}

			if (items.Length > 0)
				yield return items[^1];
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

		public static bool AreAdjacentCells(CPos a, CPos b)
		{
			var offset = b - a;
			return Math.Abs(offset.X) < 2 && Math.Abs(offset.Y) < 2;
		}

		public static IEnumerable<CPos> ExpandFootprint(CPos cell, bool allowDiagonal)
		{
			return Neighbours(cell, allowDiagonal);
		}

		public static IEnumerable<CPos> ExpandFootprint(IEnumerable<CPos> cells, bool allowDiagonal)
		{
			return cells.SelectMany(c => Neighbours(c, allowDiagonal)).Distinct();
		}

		public static IEnumerable<CPos> AdjacentCells(World w, in Target target)
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
			while (true)
			{
				var dx = r.Next(-1, 2);
				var dy = r.Next(-1, 2);

				if (dx == 0 && dy == 0)
					continue;

				p += new CVec(dx, dy);
				yield return p;
			}
		}

		public static int RandomInRange(MersenneTwister random, int[] range)
		{
			if (range.Length == 0)
				return 0;

			if (range.Length == 1)
				return range[0];

			return random.Next(range[0], range[1]);
		}

		public static string InternalTypeName(Type t)
		{
			return t.IsGenericType
				? $"{t.Name[..t.Name.IndexOf('`')]}<{string.Join(", ", t.GenericTypeArguments.Select(arg => arg.Name))}>"
				: t.Name;
		}

		public static WDist RandomDistance(MersenneTwister random, WDist[] distance)
		{
			if (distance.Length == 0)
				return WDist.Zero;

			if (distance.Length == 1)
				return distance[0];

			return new WDist(random.Next(distance[0].Length, distance[1].Length));
		}

		public static WVec RandomVector(MersenneTwister random, WVec[] vector)
		{
			if (vector.Length == 0)
				return WVec.Zero;

			if (vector.Length == 1)
				return vector[0];

			var x = random.Next(vector[0].X, vector[1].X);
			var y = random.Next(vector[0].Y, vector[1].Y);
			var z = random.Next(vector[0].Z, vector[1].Z);
			return new WVec(x, y, z);
		}

		public static string FriendlyTypeName(Type t)
		{
			if (t.IsEnum)
				return $"{t.Name} (enum)";

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>))
				return $"Set of {t.GetGenericArguments().Select(FriendlyTypeName).First()}";

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				var args = t.GetGenericArguments().Select(FriendlyTypeName).ToArray();
				return $"Dictionary with Key: {args[0]}, Value: {args[1]}";
			}

			if (t.IsSubclassOf(typeof(Array)))
				return $"Collection of {FriendlyTypeName(t.GetElementType())}";

			if (t.IsGenericType && t.GetGenericTypeDefinition().GetInterfaces().Any(e => e.IsGenericType && e.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
				return $"Collection of {FriendlyTypeName(t.GetGenericArguments().First())}";

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
				return $"{t.GetGenericArguments().Select(FriendlyTypeName).First()} (optional)";

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

			if (t == typeof(Color))
				return "Color (RRGGBB[AA] notation)";

			if (t == typeof(IProjectileInfo))
				return "Projectile";

			if (t == typeof(IWarhead))
				return "Warhead";

			return t.Name;
		}

		public static string GetAttributeParameterValue(CustomAttributeTypedArgument value)
		{
			if (value.ArgumentType.IsEnum)
				return Enum.Parse(value.ArgumentType, value.Value.ToString()).ToString();

			if (value.ArgumentType == typeof(Type) && value.Value != null)
				return (value.Value as Type).Name;

			if (value.ArgumentType.IsArray)
			{
				var names = (value.Value as IReadOnlyCollection<CustomAttributeTypedArgument>).Select(x => (x.Value as Type).Name);
				return string.Join(", ", names);
			}

			return value.Value?.ToString();
		}

		public static int GetProjectileInaccuracy(int baseInaccuracy, InaccuracyType inaccuracyType, ProjectileArgs args)
		{
			var inaccuracy = ApplyPercentageModifiers(baseInaccuracy, args.InaccuracyModifiers);
			switch (inaccuracyType)
			{
				case InaccuracyType.Maximum:
					var weaponMaxRange = ApplyPercentageModifiers(args.Weapon.Range.Length, args.RangeModifiers);
					return inaccuracy * (args.PassiveTarget - args.Source).Length / weaponMaxRange;
				case InaccuracyType.PerCellIncrement:
					return inaccuracy * (args.PassiveTarget - args.Source).Length / 1024;
				case InaccuracyType.Absolute:
					return inaccuracy;
				default:
					throw new InvalidEnumArgumentException(nameof(inaccuracyType), (int)inaccuracyType, typeof(InaccuracyType));
			}
		}
	}
}
