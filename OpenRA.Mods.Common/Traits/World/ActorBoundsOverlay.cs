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

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	[Desc("Editor overlay that draws borders around placed map actors.")]
	public class ActorBoundsOverlayInfo : TraitInfo<ActorBoundsOverlay> { }

	public class ActorBoundsOverlay : IRenderAnnotations
	{
		public bool Enabled;

		static readonly Color PlacedActorColor = Color.FromArgb(200, 165, 0, 255);

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				yield break;

			var editorActorLayer = self.World.WorldActor.Trait<EditorActorLayer>();
			foreach (var preview in editorActorLayer.PreviewsInScreenBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight))
			{
				var bounds = preview.Bounds;
				if (bounds.Width <= 0 || bounds.Height <= 0)
					continue;

				yield return new RectangleBorderAnnotationRenderable(
					new WPos(preview.CenterPosition.X, preview.CenterPosition.Y, 8192),
					bounds,
					PlacedActorColor,
					1,
					Color.Black,
					1);
			}
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
