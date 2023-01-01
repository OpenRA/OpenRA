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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class TerrainTemplatePreviewWidget : Widget
	{
		public Func<float> GetScale = () => 1f;

		readonly ITiledTerrainRenderer terrainRenderer;
		readonly WorldRenderer worldRenderer;
		readonly WorldViewportSizes viewportSizes;

		TerrainTemplateInfo template;

		public int2 PreviewOffset { get; private set; }
		public int2 IdealPreviewSize { get; private set; }

		[ObjectCreator.UseCtor]
		public TerrainTemplatePreviewWidget(ModData modData, WorldRenderer worldRenderer, World world)
		{
			this.worldRenderer = worldRenderer;
			viewportSizes = modData.Manifest.Get<WorldViewportSizes>();

			terrainRenderer = world.WorldActor.TraitOrDefault<ITiledTerrainRenderer>();
			if (terrainRenderer == null)
				throw new YamlException("TerrainTemplatePreviewWidget requires a tile-based terrain renderer.");
		}

		protected TerrainTemplatePreviewWidget(TerrainTemplatePreviewWidget other)
			: base(other)
		{
			worldRenderer = other.worldRenderer;
			viewportSizes = other.viewportSizes;
			terrainRenderer = other.terrainRenderer;
			template = other.template;
			GetScale = other.GetScale;
		}

		public override Widget Clone() { return new TerrainTemplatePreviewWidget(this); }

		public void SetTemplate(TerrainTemplateInfo template)
		{
			this.template = template;
			var b = terrainRenderer.TemplateBounds(template);
			IdealPreviewSize = new int2((int)(b.Width * viewportSizes.DefaultScale), (int)(b.Height * viewportSizes.DefaultScale));

			// Measured from the middle of the widget to the middle of the top-left cell of the template
			PreviewOffset = -new int2((int)(b.Left * viewportSizes.DefaultScale), (int)(b.Top * viewportSizes.DefaultScale)) - IdealPreviewSize / 2;
		}

		public override void Draw()
		{
			if (template == null)
				return;

			var scale = GetScale() * viewportSizes.DefaultScale;
			var origin = RenderOrigin + PreviewOffset + new int2(RenderBounds.Size.Width / 2, RenderBounds.Size.Height / 2);

			foreach (var r in terrainRenderer.RenderUIPreview(worldRenderer, template, origin, scale))
				r.PrepareRender(worldRenderer).Render(worldRenderer);
		}
	}
}
