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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorCursorLayerInfo : TraitInfo<EditorCursorLayer>, Requires<EditorActorLayerInfo>, Requires<ITiledTerrainRendererInfo> { }

	public class EditorCursorLayer : ITickRender, IRenderAboveShroud, IRenderAnnotations
	{
		IEditorBrush brush;

		static readonly IEnumerable<IRenderable> NoRenderables = [];

		public void SetBrush(IEditorBrush brush)
		{
			this.brush = brush;
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (wr.World.Type != WorldType.Editor)
				return;

			brush?.TickRender(wr, self);
		}

		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				return NoRenderables;

			return brush?.RenderAboveShroud(self, wr) ?? NoRenderables;
		}

		bool IRenderAboveShroud.SpatiallyPartitionable => false;

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				return NoRenderables;

			return brush?.RenderAnnotations(self, wr) ?? NoRenderables;
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
