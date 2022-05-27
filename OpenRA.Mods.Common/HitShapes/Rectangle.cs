#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
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
		WVec[] combatOverlayVertsSide1;
		WVec[] combatOverlayVertsSide2;

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
				new WVec(TopLeft.X, BottomRight.Y, VerticalTopOffset),
			};

			combatOverlayVertsBottom = new WVec[]
			{
				new WVec(TopLeft.X, TopLeft.Y, VerticalBottomOffset),
				new WVec(BottomRight.X, TopLeft.Y, VerticalBottomOffset),
				new WVec(BottomRight.X, BottomRight.Y, VerticalBottomOffset),
				new WVec(TopLeft.X, BottomRight.Y, VerticalBottomOffset),
			};

			combatOverlayVertsSide1 = new WVec[]
			{
				new WVec(TopLeft.X, TopLeft.Y, VerticalBottomOffset),
				new WVec(TopLeft.X, TopLeft.Y, VerticalTopOffset),
				new WVec(TopLeft.X, BottomRight.Y, VerticalTopOffset),
				new WVec(TopLeft.X, BottomRight.Y, VerticalBottomOffset),
			};

			combatOverlayVertsSide2 = new WVec[]
			{
				new WVec(BottomRight.X, TopLeft.Y, VerticalBottomOffset),
				new WVec(BottomRight.X, TopLeft.Y, VerticalTopOffset),
				new WVec(BottomRight.X, BottomRight.Y, VerticalTopOffset),
				new WVec(BottomRight.X, BottomRight.Y, VerticalBottomOffset),
			};
		}

		public WDist DistanceFromEdge(in WVec v)
		{
			var r = new WVec(
				Math.Max(Math.Abs(v.X - center.X) - quadrantSize.X, 0),
				Math.Max(Math.Abs(v.Y - center.Y) - quadrantSize.Y, 0), 0);

			return new WDist(r.HorizontalLength);
		}

		public WDist DistanceFromEdge(WPos pos, WPos origin, WRot orientation)
		{
			orientation += WRot.FromYaw(LocalYaw);

			if (pos.Z > origin.Z + VerticalTopOffset)
				return DistanceFromEdge((pos - (origin + new WVec(0, 0, VerticalTopOffset))).Rotate(-orientation));

			if (pos.Z < origin.Z + VerticalBottomOffset)
				return DistanceFromEdge((pos - (origin + new WVec(0, 0, VerticalBottomOffset))).Rotate(-orientation));

			return DistanceFromEdge((pos - new WPos(origin.X, origin.Y, pos.Z)).Rotate(-orientation));
		}

		IEnumerable<IRenderable> IHitShape.RenderDebugOverlay(HitShape hs, WorldRenderer wr, WPos origin, WRot orientation)
		{
			orientation += WRot.FromYaw(LocalYaw);

			var vertsTop = combatOverlayVertsTop.Select(v => origin + v.Rotate(orientation)).ToArray();
			var vertsBottom = combatOverlayVertsBottom.Select(v => origin + v.Rotate(orientation)).ToArray();
			var side1 = combatOverlayVertsSide1.Select(v => origin + v.Rotate(orientation)).ToArray();
			var side2 = combatOverlayVertsSide2.Select(v => origin + v.Rotate(orientation)).ToArray();

			var shapeColor = hs.IsTraitDisabled ? Color.LightGray : Color.Yellow;

			yield return new PolygonAnnotationRenderable(vertsTop, origin, 1, shapeColor);
			yield return new PolygonAnnotationRenderable(vertsBottom, origin, 1, shapeColor);
			yield return new PolygonAnnotationRenderable(side1, origin, 1, shapeColor);
			yield return new PolygonAnnotationRenderable(side2, origin, 1, shapeColor);
			yield return new CircleAnnotationRenderable(origin, OuterRadius, 1, hs.IsTraitDisabled ? Color.Gray : Color.LimeGreen);
		}
	}
}
