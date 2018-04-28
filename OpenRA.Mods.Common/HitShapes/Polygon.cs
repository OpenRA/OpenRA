#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.HitShapes
{
	public class PolygonShape : IHitShape
	{
		public WDist OuterRadius { get; private set; }

		[FieldLoader.Require]
		public readonly int2[] Points;

		[Desc("Defines the top offset relative to the actor's center.")]
		public readonly int VerticalTopOffset = 0;

		[Desc("Defines the bottom offset relative to the actor's center.")]
		public readonly int VerticalBottomOffset = 0;

		[Desc("Rotates shape by an angle relative to actor facing. Mostly required for buildings on isometric terrain.",
			"Mobile actors do NOT need this!")]
		public readonly WAngle LocalYaw = WAngle.Zero;

		WVec[] combatOverlayVertsTop;
		WVec[] combatOverlayVertsBottom;
		int[] squares;
		int2[] pointsInner;

		public PolygonShape() { }

		public PolygonShape(int2[] points) { Points = points; }

		public void Initialize()
		{
			if (VerticalTopOffset < VerticalBottomOffset)
				throw new YamlException("VerticalTopOffset must be equal to or higher than VerticalBottomOffset.");

			// Internally, negative X values mean forward, so we need to convert the values set by the modder accordingly
			pointsInner = Points.Select(p => new int2(-p.X, p.Y)).ToArray();

			OuterRadius = new WDist(pointsInner.Max(x => x.Length));
			combatOverlayVertsTop = pointsInner.Select(p => new WVec(p.X, p.Y, VerticalTopOffset)).ToArray();
			combatOverlayVertsBottom = pointsInner.Select(p => new WVec(p.X, p.Y, VerticalBottomOffset)).ToArray();
			squares = new int[pointsInner.Length];
			squares[0] = (pointsInner[0] - pointsInner[pointsInner.Length - 1]).LengthSquared;
			for (var i = 1; i < pointsInner.Length; i++)
				squares[i] = (pointsInner[i] - pointsInner[i - 1]).LengthSquared;
		}

		static int DistanceSquaredFromLineSegment(int2 c, int2 a, int2 b, int ab2)
		{
			var ac = c - a;
			var ac2 = ac.LengthSquared;
			var bc2 = (c - b).LengthSquared;

			// c is closest to point a
			if (ac2 + ab2 <= bc2)
				return ac2;

			// c is closest to point b
			if (bc2 + ab2 <= ac2)
				return bc2;

			// c is closest to its unknown orthogonal projection (p) onto the line spanned by b with a as the origin
			// Cast to a long for the calculations to avoid overflows
			var ab = b - a;
			var ap2 = ac.X * ab.X + ac.Y * ab.Y;
			var ap = new int2((int)((long)ab.X * ap2 / ab2), (int)((long)ab.Y * ap2 / ab2));

			// Length of vector pc squared.
			return (ac - ap).LengthSquared;
		}

		public WDist DistanceFromEdge(WVec v)
		{
			var p = new int2(v.X, v.Y);
			var z = Math.Abs(v.Z);
			if (pointsInner.PolygonContains(p))
				return new WDist(z);

			var min2 = DistanceSquaredFromLineSegment(p, pointsInner[pointsInner.Length - 1], pointsInner[0], squares[0]);
			for (var i = 1; i < pointsInner.Length; i++)
			{
				var d2 = DistanceSquaredFromLineSegment(p, pointsInner[i - 1], pointsInner[i], squares[i]);
				if (d2 < min2)
					min2 = d2;
			}

			return new WDist(Exts.ISqrt(min2 + z * z));
		}

		public WDist DistanceFromEdge(WPos pos, Actor actor)
		{
			var actorPos = actor.CenterPosition;
			var orientation = actor.Orientation + WRot.FromYaw(LocalYaw);

			if (pos.Z > actorPos.Z + VerticalTopOffset)
				return DistanceFromEdge((pos - (actorPos + new WVec(0, 0, VerticalTopOffset))).Rotate(-orientation));

			if (pos.Z < actorPos.Z + VerticalBottomOffset)
				return DistanceFromEdge((pos - (actorPos + new WVec(0, 0, VerticalBottomOffset))).Rotate(-orientation));

			return DistanceFromEdge((pos - new WPos(actorPos.X, actorPos.Y, pos.Z)).Rotate(-orientation));
		}

		public void DrawCombatOverlay(WorldRenderer wr, RgbaColorRenderer wcr, Actor actor)
		{
			var actorPos = actor.CenterPosition;
			var orientation = actor.Orientation + WRot.FromYaw(LocalYaw);

			var vertsTop = combatOverlayVertsTop.Select(v => wr.Screen3DPosition(actorPos + v.Rotate(orientation)));
			var vertsBottom = combatOverlayVertsBottom.Select(v => wr.Screen3DPosition(actorPos + v.Rotate(orientation)));
			wcr.DrawPolygon(vertsTop.ToArray(), 1, Color.Yellow);
			wcr.DrawPolygon(vertsBottom.ToArray(), 1, Color.Yellow);

			RangeCircleRenderable.DrawRangeCircle(wr, actorPos, OuterRadius, 1, Color.LimeGreen, 0, Color.LimeGreen);
		}
	}
}
