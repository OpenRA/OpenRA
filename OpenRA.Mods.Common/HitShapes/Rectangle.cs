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
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.HitShapes
{
	public class RectangleShape : IHitShape
	{
		public WDist OuterRadius { get; private set; }

		[FieldLoader.Require]
		public readonly int2 TopLeft;

		[FieldLoader.Require]
		public readonly int2 BottomRight;

		int2 quadrantSize;
		int2 center;

		WVec[] combatOverlayVerts;

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

			quadrantSize = (BottomRight - TopLeft) / 2;
			center = TopLeft + quadrantSize;

			OuterRadius = new WDist(Math.Max(TopLeft.Length, BottomRight.Length));

			combatOverlayVerts = new WVec[]
			{
				new WVec(TopLeft.X, TopLeft.Y, 0),
				new WVec(BottomRight.X, TopLeft.Y, 0),
				new WVec(BottomRight.X, BottomRight.Y, 0),
				new WVec(TopLeft.X, BottomRight.Y, 0)
			};
		}

		public WDist DistanceFromEdge(WVec v)
		{
			var r = new int2(
				Math.Max(Math.Abs(v.X - center.X) - quadrantSize.X, 0),
				Math.Max(Math.Abs(v.Y - center.Y) - quadrantSize.Y, 0));

			return new WDist(r.Length);
		}

		public WDist DistanceFromEdge(WPos pos, Actor actor)
		{
			return DistanceFromEdge((pos - actor.CenterPosition).Rotate(-actor.Orientation));
		}

		public void DrawCombatOverlay(WorldRenderer wr, RgbaColorRenderer wcr, Actor actor)
		{
			var verts = combatOverlayVerts.Select(v => wr.ScreenPosition(actor.CenterPosition + v.Rotate(actor.Orientation)));
			wcr.DrawPolygon(verts.ToArray(), 1, Color.Yellow);
		}
	}
}
