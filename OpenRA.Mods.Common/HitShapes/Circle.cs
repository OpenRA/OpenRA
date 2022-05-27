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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.HitShapes
{
	public class CircleShape : IHitShape
	{
		public WDist OuterRadius => Radius;

		[FieldLoader.Require]
		public readonly WDist Radius = new WDist(426);

		[Desc("Defines the top offset relative to the actor's center.")]
		public readonly int VerticalTopOffset = 0;

		[Desc("Defines the bottom offset relative to the actor's center.")]
		public readonly int VerticalBottomOffset = 0;

		public CircleShape() { }

		public CircleShape(WDist radius) { Radius = radius; }

		public void Initialize()
		{
			if (VerticalTopOffset < VerticalBottomOffset)
				throw new YamlException("VerticalTopOffset must be equal to or higher than VerticalBottomOffset.");
		}

		public WDist DistanceFromEdge(in WVec v)
		{
			return new WDist(Math.Max(0, v.Length - Radius.Length));
		}

		public WDist DistanceFromEdge(WPos pos, WPos origin, WRot orientation)
		{
			if (pos.Z > origin.Z + VerticalTopOffset)
				return DistanceFromEdge(pos - (origin + new WVec(0, 0, VerticalTopOffset)));

			if (pos.Z < origin.Z + VerticalBottomOffset)
				return DistanceFromEdge(pos - (origin + new WVec(0, 0, VerticalBottomOffset)));

			return DistanceFromEdge(pos - new WPos(origin.X, origin.Y, pos.Z));
		}

		IEnumerable<IRenderable> IHitShape.RenderDebugOverlay(HitShape hs, WorldRenderer wr, WPos origin, WRot orientation)
		{
			var shapeColor = hs.IsTraitDisabled ? Color.LightGray : Color.Yellow;
			yield return new CircleAnnotationRenderable(origin + new WVec(0, 0, VerticalTopOffset), Radius, 1, shapeColor);
			yield return new CircleAnnotationRenderable(origin + new WVec(0, 0, VerticalBottomOffset), Radius, 1, shapeColor);
		}
	}
}
