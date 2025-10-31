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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders polygon for mouse bounds (usually defined by " + nameof(Interactable) + " or " + nameof(Selectable) + ").",
		"Put on actor for which the polygon should be rendered.")]
	public class RenderMouseBoundsInfo : TraitInfo, Requires<InteractableInfo>
	{
		[Desc("Color to use for the polygon lines.")]
		public readonly Color PolygonLineColor = Color.Green;

		public override object Create(ActorInitializer init) { return new RenderMouseBounds(init.Self, this); }
	}

	public class RenderMouseBounds : IRenderAnnotations
	{
		readonly IMouseBounds mouseBounds;
		readonly RenderMouseBoundsInfo info;

		public RenderMouseBounds(Actor self, RenderMouseBoundsInfo info)
		{
			mouseBounds = self.Trait<IMouseBounds>();
			this.info = info;
		}

		bool IRenderAnnotations.SpatiallyPartitionable => true;

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (self.World.FogObscures(self))
				return [];

			return DrawDecorations(self, wr);
		}

		IEnumerable<IRenderable> DrawDecorations(Actor self, WorldRenderer wr)
		{
			var polygon = mouseBounds.MouseoverBounds(self, wr);
			var vertices = new WPos[polygon.Vertices.Length];

			for (var i = 0; i < polygon.Vertices.Length; i++)
			{
				var screenVertex = polygon.Vertices[i];

				vertices[i] = wr.ProjectedPosition(screenVertex);
			}

			yield return new PolygonAnnotationRenderable(vertices, vertices[0], 1, info.PolygonLineColor);
		}
	}
}
