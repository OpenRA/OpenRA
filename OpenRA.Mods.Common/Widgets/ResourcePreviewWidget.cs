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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ResourcePreviewWidget : Widget
	{
		public Func<float> GetScale = () => 1f;

		readonly WorldRenderer worldRenderer;
		readonly IResourceRenderer[] resourceRenderers;
		readonly Size tileSize;

		string resourceType;
		IResourceRenderer resourceRenderer;

		public string ResourceType
		{
			get => resourceType;

			set
			{
				resourceType = value;
				if (resourceType != null)
					resourceRenderer = resourceRenderers.FirstOrDefault(r => r.ResourceTypes.Contains(resourceType));
				else
					resourceRenderer = null;
			}
		}

		[ObjectCreator.UseCtor]
		public ResourcePreviewWidget(WorldRenderer worldRenderer, World world)
		{
			this.worldRenderer = worldRenderer;
			resourceRenderers = world.WorldActor.TraitsImplementing<IResourceRenderer>().ToArray();
			tileSize = world.Map.Grid.TileSize;
		}

		protected ResourcePreviewWidget(ResourcePreviewWidget other)
			: base(other)
		{
			GetScale = other.GetScale;
			worldRenderer = other.worldRenderer;
			resourceRenderers = other.resourceRenderers;
			tileSize = other.tileSize;
			resourceType = other.resourceType;
			resourceRenderer = other.resourceRenderer;
		}

		public override Widget Clone() { return new ResourcePreviewWidget(this); }

		public override void Draw()
		{
			if (resourceRenderer == null)
				return;

			var scale = GetScale();
			var origin = RenderOrigin + new int2((RenderBounds.Size.Width - tileSize.Width) / 2, (RenderBounds.Size.Height - tileSize.Height) / 2);
			foreach (var r in resourceRenderer.RenderUIPreview(worldRenderer, resourceType, origin, scale))
				r.PrepareRender(worldRenderer).Render(worldRenderer);
		}
	}
}
