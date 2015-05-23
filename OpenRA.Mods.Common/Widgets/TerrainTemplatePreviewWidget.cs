#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class TerrainTemplatePreviewWidget : Widget
	{
		public Func<float> GetScale = () => 1f;
		public string Palette = "terrain";

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

				var ts = Game.ModData.Manifest.TileSize;
				var shape = Game.ModData.Manifest.TileShape;
				bounds = worldRenderer.Theater.TemplateBounds(template, ts, shape);
			}
		}

		[ObjectCreator.UseCtor]
		public TerrainTemplatePreviewWidget(WorldRenderer worldRenderer, World world)
		{
			this.worldRenderer = worldRenderer;
			tileset = world.Map.Rules.TileSets[world.Map.Tileset];
		}

		protected TerrainTemplatePreviewWidget(TerrainTemplatePreviewWidget other)
			: base(other)
		{
			worldRenderer = other.worldRenderer;
			tileset = other.worldRenderer.World.Map.Rules.TileSets[other.worldRenderer.World.Map.Tileset];
			Template = other.Template;
			GetScale = other.GetScale;
		}

		public override Widget Clone() { return new TerrainTemplatePreviewWidget(this); }

		public override void Draw()
		{
			if (template == null)
				return;

			var ts = Game.ModData.Manifest.TileSize;
			var shape = Game.ModData.Manifest.TileShape;
			var scale = GetScale();

			var sb = new Rectangle((int)(scale * bounds.X), (int)(scale * bounds.Y), (int)(scale * bounds.Width), (int)(scale * bounds.Height));
			var origin = RenderOrigin + new int2((RenderBounds.Size.Width - sb.Width) / 2 - sb.X, (RenderBounds.Size.Height - sb.Height) / 2 - sb.Y);

			var i = 0;
			for (var y = 0; y < Template.Size.Y; y++)
			{
				for (var x = 0; x < Template.Size.X; x++)
				{
					var tile = new TerrainTile(Template.Id, (byte)(i++));
					var tileInfo = tileset.GetTileInfo(tile);

					// Empty tile
					if (tileInfo == null)
						continue;

					var sprite = worldRenderer.Theater.TileSprite(tile);
					var size = new float2(sprite.Size.X * scale, sprite.Size.Y * scale);

					var u = shape == TileShape.Rectangle ? x : (x - y) / 2f;
					var v = shape == TileShape.Rectangle ? y : (x + y) / 2f;
					var pos = origin + scale * (new float2(u * ts.Width, (v - 0.5f * tileInfo.Height) * ts.Height) - 0.5f * sprite.Size);
					Game.Renderer.SpriteRenderer.DrawSprite(sprite, pos, worldRenderer.Palette(Palette), size);
				}
			}
		}
	}
}
