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
		public Func<float> GetScale = () => 1f;

		readonly WorldRenderer worldRenderer;
		readonly WorldViewportSizes viewportSizes;
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

		public Size IdealPreviewSize { get; private set; }

		[ObjectCreator.UseCtor]
		public ResourcePreviewWidget(ModData modData, WorldRenderer worldRenderer, World world)
		{
			this.worldRenderer = worldRenderer;
			viewportSizes = modData.Manifest.Get<WorldViewportSizes>();
			resourceRenderers = world.WorldActor.TraitsImplementing<IResourceRenderer>().ToArray();
			tileSize = world.Map.Grid.TileSize;
			IdealPreviewSize = new Size(
				(int)(viewportSizes.DefaultScale * tileSize.Width),
				(int)(viewportSizes.DefaultScale * tileSize.Height));
		}

		protected ResourcePreviewWidget(ResourcePreviewWidget other)
			: base(other)
		{
			GetScale = other.GetScale;
			worldRenderer = other.worldRenderer;
			viewportSizes = other.viewportSizes;
			resourceRenderers = other.resourceRenderers;
			tileSize = other.tileSize;
			resourceType = other.resourceType;
			resourceRenderer = other.resourceRenderer;
			IdealPreviewSize = other.IdealPreviewSize;
		}

		public override Widget Clone() { return new ResourcePreviewWidget(this); }

		public override void Draw()
		{
			if (resourceRenderer == null)
				return;

			var scale = GetScale() * viewportSizes.DefaultScale;
			var origin = RenderOrigin + new int2(
				(int)(0.5f * (RenderBounds.Size.Width - scale * tileSize.Width)),
				(int)(0.5f * (RenderBounds.Size.Height - scale * tileSize.Height)));

			foreach (var r in resourceRenderer.RenderUIPreview(worldRenderer, resourceType, origin, scale))
				r.PrepareRender(worldRenderer).Render(worldRenderer);
		}
	}
}
