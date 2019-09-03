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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.HitShapes
{
	public class CircleShape : IHitShape
	{
		public WDist OuterRadius { get { return Radius; } }

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

		public WDist DistanceFromEdge(WVec v)
		{
			return new WDist(Math.Max(0, v.Length - Radius.Length));
		}

		public WDist DistanceFromEdge(WPos pos, Actor actor)
		{
			var actorPos = actor.CenterPosition;

			if (pos.Z > actorPos.Z + VerticalTopOffset)
				return DistanceFromEdge(pos - (actorPos + new WVec(0, 0, VerticalTopOffset)));

			if (pos.Z < actorPos.Z + VerticalBottomOffset)
				return DistanceFromEdge(pos - (actorPos + new WVec(0, 0, VerticalBottomOffset)));

			return DistanceFromEdge(pos - new WPos(actorPos.X, actorPos.Y, pos.Z));
		}

		IEnumerable<IRenderable> IHitShape.RenderDebugOverlay(WorldRenderer wr, Actor actor)
		{
			var actorPos = actor.CenterPosition;
			yield return new CircleAnnotationRenderable(actorPos + new WVec(0, 0, VerticalTopOffset), Radius, 1, Color.Yellow);
			yield return new CircleAnnotationRenderable(actorPos + new WVec(0, 0, VerticalBottomOffset), Radius, 1, Color.Yellow);
		}
	}
}
