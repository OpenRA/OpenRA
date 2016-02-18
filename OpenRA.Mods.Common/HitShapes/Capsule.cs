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
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;

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
			return DistanceFromEdge((pos - actor.CenterPosition).Rotate(-actor.Orientation));
		}

		public void DrawCombatOverlay(WorldRenderer wr, RgbaColorRenderer wcr, Actor actor)
		{
			var a = actor.CenterPosition + new WVec(PointA.X, PointA.Y, 0).Rotate(actor.Orientation);
			var b = actor.CenterPosition + new WVec(PointB.X, PointB.Y, 0).Rotate(actor.Orientation);

			var offset = new WVec(a.Y - b.Y, b.X - a.X, 0);
			offset = offset * Radius.Length / offset.Length;

			var c = Color.Yellow;
			RangeCircleRenderable.DrawRangeCircle(wr, a, Radius, 1, c, 0, c);
			RangeCircleRenderable.DrawRangeCircle(wr, b, Radius, 1, c, 0, c);
			wcr.DrawLine(new[] { wr.ScreenPosition(a - offset), wr.ScreenPosition(b - offset) }, 1, c);
			wcr.DrawLine(new[] { wr.ScreenPosition(a + offset), wr.ScreenPosition(b + offset) }, 1, c);
		}
	}
}