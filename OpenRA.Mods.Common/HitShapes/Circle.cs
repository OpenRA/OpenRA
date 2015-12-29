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
	public class CircleShape : IHitShape
	{
		public WDist OuterRadius { get { return Radius; } }

		[FieldLoader.Require]
		public readonly WDist Radius = new WDist(426);

		public CircleShape() { }

		public CircleShape(WDist radius) { Radius = radius; }

		public void Initialize() { }

		public WDist DistanceFromEdge(WVec v)
		{
			return new WDist(Math.Max(0, v.Length - Radius.Length));
		}

		public WDist DistanceFromEdge(WPos pos, Actor actor)
		{
			return DistanceFromEdge(pos - actor.CenterPosition);
		}

		public void DrawCombatOverlay(WorldRenderer wr, RgbaColorRenderer wcr, Actor actor)
		{
			RangeCircleRenderable.DrawRangeCircle(wr, actor.CenterPosition, Radius, 1, Color.Yellow, 0, Color.Yellow);
		}
	}
}
