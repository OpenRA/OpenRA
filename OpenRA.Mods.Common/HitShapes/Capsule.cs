#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.HitShapes
{
	public class CapsuleShape : IHitShape
	{
		public WDist OuterRadius { get; private set; }

		[FieldLoader.Require]
		public readonly int2 PointA;

		[FieldLoader.Require]
		public readonly int2 PointB;

		public readonly WDist Radius = new WDist(426);

		[Desc("Defines the top offset relative to the actor's target point.")]
		public readonly int VerticalTopOffset = 0;

		[Desc("Defines the bottom offset relative to the actor's target point.")]
		public readonly int VerticalBottomOffset = 0;

		int2 ab;
		int abLenSq;

		public CapsuleShape() { }

		public CapsuleShape(int2 a, int2 b, WDist radius)
		{
			PointA = a;
			PointB = b;
			Radius = radius;
		}

		public void Initialize()
		{
			ab = PointB - PointA;
			abLenSq = ab.LengthSquared / 1024;

			if (abLenSq == 0)
				throw new YamlException("This Capsule describes a circle. Use a Circle HitShape instead.");

			if (VerticalTopOffset < VerticalBottomOffset)
				throw new YamlException("VerticalTopOffset must be equal to or higher than VerticalBottomOffset.");

			OuterRadius = Radius + new WDist(Math.Max(PointA.Length, PointB.Length));
		}

		public WDist DistanceFromEdge(WVec v)
		{
			var p = new int2(v.X, v.Y);

			var t = int2.Dot(p - PointA, ab) / abLenSq;

			if (t < 0)
				return new WDist(Math.Max(0, (PointA - p).Length - Radius.Length));
			if (t > 1024)
				return new WDist(Math.Max(0, (PointB - p).Length - Radius.Length));

			var projection = PointA + new int2(
				(ab.X * t) / 1024,
				(ab.Y * t) / 1024);

			var distance = (projection - p).Length;

			return new WDist(Math.Max(0, distance - Radius.Length));
		}

		public WDist DistanceFromEdge(WPos pos, Actor actor)
		{
			var actorPos = actor.CenterPosition;

			if (pos.Z > actorPos.Z + VerticalTopOffset)
				return DistanceFromEdge((pos - (actorPos + new WVec(0, 0, VerticalTopOffset))).Rotate(-actor.Orientation));

			if (pos.Z < actorPos.Z + VerticalBottomOffset)
				return DistanceFromEdge((pos - (actorPos + new WVec(0, 0, VerticalBottomOffset))).Rotate(-actor.Orientation));

			return DistanceFromEdge((pos - new WPos(actorPos.X, actorPos.Y, pos.Z)).Rotate(-actor.Orientation));
		}

		public void DrawCombatOverlay(WorldRenderer wr, RgbaColorRenderer wcr, Actor actor)
		{
			var actorPos = actor.CenterPosition;

			var a = actorPos + new WVec(PointA.X, PointA.Y, VerticalTopOffset).Rotate(actor.Orientation);
			var b = actorPos + new WVec(PointB.X, PointB.Y, VerticalTopOffset).Rotate(actor.Orientation);
			var aa = actorPos + new WVec(PointA.X, PointA.Y, VerticalBottomOffset).Rotate(actor.Orientation);
			var bb = actorPos + new WVec(PointB.X, PointB.Y, VerticalBottomOffset).Rotate(actor.Orientation);

			var offset1 = new WVec(a.Y - b.Y, b.X - a.X, 0);
			offset1 = offset1 * Radius.Length / offset1.Length;
			var offset2 = new WVec(aa.Y - bb.Y, bb.X - aa.X, 0);
			offset2 = offset2 * Radius.Length / offset2.Length;

			var c = Color.Yellow;
			RangeCircleRenderable.DrawRangeCircle(wr, a, Radius, 1, c, 0, c);
			RangeCircleRenderable.DrawRangeCircle(wr, b, Radius, 1, c, 0, c);
			wcr.DrawLine(new[] { wr.Screen3DPosition(a - offset1), wr.Screen3DPosition(b - offset1) }, 1, c);
			wcr.DrawLine(new[] { wr.Screen3DPosition(a + offset1), wr.Screen3DPosition(b + offset1) }, 1, c);

			RangeCircleRenderable.DrawRangeCircle(wr, aa, Radius, 1, c, 0, c);
			RangeCircleRenderable.DrawRangeCircle(wr, bb, Radius, 1, c, 0, c);
			wcr.DrawLine(new[] { wr.Screen3DPosition(aa - offset2), wr.Screen3DPosition(bb - offset2) }, 1, c);
			wcr.DrawLine(new[] { wr.Screen3DPosition(aa + offset2), wr.Screen3DPosition(bb + offset2) }, 1, c);
		}
	}
}