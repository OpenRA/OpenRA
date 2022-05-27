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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class TerrainTemplatePreviewWidget : Widget
	{
		public Func<float> GetScale = () => 1f;

		readonly ITiledTerrainRenderer terrainRenderer;
		readonly WorldRenderer worldRenderer;

		TerrainTemplateInfo template;
		Rectangle bounds;

		public TerrainTemplateInfo Template
		{
			get => template;

			set
			{
				template = value;
				if (template == null)
					return;

				bounds = terrainRenderer.TemplateBounds(template);
			}
		}

		[ObjectCreator.UseCtor]
		public TerrainTemplatePreviewWidget(WorldRenderer worldRenderer, World world)
		{
			this.worldRenderer = worldRenderer;
			terrainRenderer = world.WorldActor.TraitOrDefault<ITiledTerrainRenderer>();
			if (terrainRenderer == null)
				throw new YamlException("TerrainTemplatePreviewWidget requires a tile-based terrain renderer.");
		}

		protected TerrainTemplatePreviewWidget(TerrainTemplatePreviewWidget other)
			: base(other)
		{
			worldRenderer = other.worldRenderer;
			terrainRenderer = other.terrainRenderer;
			Template = other.Template;
			GetScale = other.GetScale;
		}

		public override Widget Clone() { return new TerrainTemplatePreviewWidget(this); }

		public override void Draw()
		{
			if (template == null)
				return;

			var scale = GetScale();
			var sb = new Rectangle((int)(scale * bounds.X), (int)(scale * bounds.Y), (int)(scale * bounds.Width), (int)(scale * bounds.Height));
			var origin = RenderOrigin + new int2((RenderBounds.Size.Width - sb.Width) / 2 - sb.X, (RenderBounds.Size.Height - sb.Height) / 2 - sb.Y);
			foreach (var r in terrainRenderer.RenderUIPreview(worldRenderer, template, origin, scale))
				r.PrepareRender(worldRenderer).Render(worldRenderer);
		}
	}
}
