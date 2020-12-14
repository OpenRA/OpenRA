#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class TerrainTemplatePreviewWidget : Widget
	{
		public Func<float> GetScale = () => 1f;

		readonly WorldRenderer worldRenderer;
		readonly TileSet tileset;

		TerrainTemplateInfo template;
		Rectangle bounds;

		public TerrainTemplateInfo Template
		{
			get
			{
				return template;
			}

			set
			{
				template = value;
				if (template == null)
					return;

				var grid = Game.ModData.Manifest.Get<MapGrid>();
				bounds = worldRenderer.Theater.TemplateBounds(template, grid.TileSize, grid.Type);
			}
		}

		[ObjectCreator.UseCtor]
		public TerrainTemplatePreviewWidget(WorldRenderer worldRenderer, World world)
		{
			this.worldRenderer = worldRenderer;
			tileset = world.Map.Rules.TileSet;
		}

		protected TerrainTemplatePreviewWidget(TerrainTemplatePreviewWidget other)
			: base(other)
		{
			worldRenderer = other.worldRenderer;
			tileset = other.worldRenderer.World.Map.Rules.TileSet;
			Template = other.Template;
			GetScale = other.GetScale;
		}

		public override Widget Clone() { return new TerrainTemplatePreviewWidget(this); }

		public override void Draw()
		{
			if (template == null)
				return;

			var grid = Game.ModData.Manifest.Get<MapGrid>();
			var ts = grid.TileSize;
			var gridType = grid.Type;
			var scale = GetScale();

			var sb = new Rectangle((int)(scale * bounds.X), (int)(scale * bounds.Y), (int)(scale * bounds.Width), (int)(scale * bounds.Height));
			var origin = RenderOrigin + new int2((RenderBounds.Size.Width - sb.Width) / 2 - sb.X, (RenderBounds.Size.Height - sb.Height) / 2 - sb.Y);

			var i = 0;
			for (var y = 0; y < Template.Size.Y; y++)
			{
				for (var x = 0; x < Template.Size.X; x++)
				{
					var tile = new TerrainTile(Template.Id, (byte)(i++));
					if (!tileset.TryGetTileInfo(tile, out var tileInfo))
						continue;

					var sprite = worldRenderer.Theater.TileSprite(tile, 0);
					var size = new float2(sprite.Size.X * scale, sprite.Size.Y * scale);

					var u = gridType == MapGridType.Rectangular ? x : (x - y) / 2f;
					var v = gridType == MapGridType.Rectangular ? y : (x + y) / 2f;
					var pos = origin + scale * (new float2(u * ts.Width, (v - 0.5f * tileInfo.Height) * ts.Height) - 0.5f * sprite.Size);
					var palette = Template.Palette ?? TileSet.TerrainPaletteInternalName;
					Game.Renderer.SpriteRenderer.DrawSprite(sprite, pos, worldRenderer.Palette(palette), size);
				}
			}
		}
	}
}
