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
		public float Scale = 1f;

		public Size IdealPreviewSize { get; private set; }

		readonly WorldRenderer worldRenderer;
		readonly WorldViewportSizes viewportSizes;
		readonly IResourceRenderer[] resourceRenderers;
		readonly Size tileSize;

		string resourceType;
		IResourceRenderer resourceRenderer;
		int2 spriteOffset;

		public void SetResourceType(string value)
		{
			resourceType = value;
			if (resourceType != null)
				resourceRenderer = resourceRenderers.FirstOrDefault(r => r.ResourceTypes.Contains(resourceType));
			else
				resourceRenderer = null;

			var bounds = resourceRenderer?.RenderUIPreview(worldRenderer, resourceType, int2.Zero, viewportSizes.DefaultScale)
				.FirstOrDefault()
				.PrepareRender(worldRenderer)
				?.ScreenBounds(worldRenderer);

			if (bounds != null)
			{
				spriteOffset = -bounds.Value.Location;
				IdealPreviewSize = bounds.Value.Size;
			}
			else
				IdealPreviewSize = new Size((int)(tileSize.Width * viewportSizes.DefaultScale), (int)(tileSize.Height * viewportSizes.DefaultScale));
		}

		[ObjectCreator.UseCtor]
		public ResourcePreviewWidget(ModData modData, WorldRenderer worldRenderer, World world)
		{
			this.worldRenderer = worldRenderer;
			viewportSizes = modData.GetOrCreate<WorldViewportSizes>();
			resourceRenderers = world.WorldActor.TraitsImplementing<IResourceRenderer>().ToArray();
			tileSize = world.Map.Rules.TerrainInfo.TileSize;
		}

		protected ResourcePreviewWidget(ResourcePreviewWidget other)
			: base(other)
		{
			Scale = other.Scale;
			worldRenderer = other.worldRenderer;
			viewportSizes = other.viewportSizes;
			resourceRenderers = other.resourceRenderers;
			tileSize = other.tileSize;
			resourceType = other.resourceType;
			resourceRenderer = other.resourceRenderer;
			IdealPreviewSize = other.IdealPreviewSize;
		}

		public override ResourcePreviewWidget Clone() { return new ResourcePreviewWidget(this); }

		public override void Draw()
		{
			if (resourceRenderer == null)
				return;

			var scale = Scale * viewportSizes.DefaultScale;
			foreach (var r in resourceRenderer.RenderUIPreview(worldRenderer, resourceType, RenderOrigin + spriteOffset, scale))
				r.PrepareRender(worldRenderer).Render(worldRenderer);
		}
	}
}
