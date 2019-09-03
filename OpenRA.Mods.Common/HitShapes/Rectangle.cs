#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.HitShapes
{
	public class RectangleShape : IHitShape
	{
		public WDist OuterRadius { get; private set; }

		[FieldLoader.Require]
		public readonly int2 TopLeft;

		[FieldLoader.Require]
		public readonly int2 BottomRight;

		[Desc("Defines the top offset relative to the actor's center.")]
		public readonly int VerticalTopOffset = 0;

		[Desc("Defines the bottom offset relative to the actor's center.")]
		public readonly int VerticalBottomOffset = 0;

		[Desc("Rotates shape by an angle relative to actor facing. Mostly required for buildings on isometric terrain.",
			"Mobile actors do NOT need this!")]
		public readonly WAngle LocalYaw = WAngle.Zero;

		int2 quadrantSize;
		int2 center;

		WVec[] combatOverlayVertsTop;
		WVec[] combatOverlayVertsBottom;

		public RectangleShape() { }

		public RectangleShape(int2 tl, int2 br)
		{
			TopLeft = tl;
			BottomRight = br;
		}

		public void Initialize()
		{
			if (TopLeft.X >= BottomRight.X || TopLeft.Y >= BottomRight.Y)
				throw new YamlException("TopLeft and BottomRight points are invalid.");

			if (VerticalTopOffset < VerticalBottomOffset)
				throw new YamlException("VerticalTopOffset must be equal to or higher than VerticalBottomOffset.");

			quadrantSize = (BottomRight - TopLeft) / 2;
			center = TopLeft + quadrantSize;

			var topRight = new int2(BottomRight.X, TopLeft.Y);
			var bottomLeft = new int2(TopLeft.X, BottomRight.Y);
			var corners = new[] { TopLeft, BottomRight, topRight, bottomLeft };
			OuterRadius = new WDist(corners.Select(x => x.Length).Max());

			combatOverlayVertsTop = new WVec[]
			{
				new WVec(TopLeft.X, TopLeft.Y, VerticalTopOffset),
				new WVec(BottomRight.X, TopLeft.Y, VerticalTopOffset),
				new WVec(BottomRight.X, BottomRight.Y, VerticalTopOffset),
				new WVec(TopLeft.X, BottomRight.Y, VerticalTopOffset)
			};

			combatOverlayVertsBottom = new WVec[]
			{
				new WVec(TopLeft.X, TopLeft.Y, VerticalBottomOffset),
				new WVec(BottomRight.X, TopLeft.Y, VerticalBottomOffset),
				new WVec(BottomRight.X, BottomRight.Y, VerticalBottomOffset),
				new WVec(TopLeft.X, BottomRight.Y, VerticalBottomOffset)
			};
		}

		public WDist DistanceFromEdge(WVec v)
		{
			var r = new WVec(
				Math.Max(Math.Abs(v.X - center.X) - quadrantSize.X, 0),
				Math.Max(Math.Abs(v.Y - center.Y) - quadrantSize.Y, 0), 0);

			return new WDist(r.HorizontalLength);
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

		IEnumerable<IRenderable> IHitShape.RenderDebugOverlay(WorldRenderer wr, Actor actor)
		{
			var actorPos = actor.CenterPosition;
			var orientation = actor.Orientation + WRot.FromYaw(LocalYaw);

			var vertsTop = combatOverlayVertsTop.Select(v => actorPos + v.Rotate(orientation)).ToArray();
			var vertsBottom = combatOverlayVertsBottom.Select(v => actorPos + v.Rotate(orientation)).ToArray();

			yield return new PolygonAnnotationRenderable(vertsTop, actorPos, 1, Color.Yellow);
			yield return new PolygonAnnotationRenderable(vertsBottom, actorPos, 1, Color.Yellow);
			yield return new CircleAnnotationRenderable(actorPos, OuterRadius, 1, Color.LimeGreen);
		}
	}
}
