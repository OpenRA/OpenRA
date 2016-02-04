#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using System.Collections.Generic;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorTileAutoappendBrush : IEditorBrush
	{
		public readonly ushort Template;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorHighlightLayer highlightLayer;
		readonly Rectangle bounds;

		List<TerrainTemplatePreviewWidget> path = new List<TerrainTemplatePreviewWidget>();
		TerrainTemplatePreviewWidget preview;

		readonly ushort[][] similarTilesets = new ushort[][] {
			new ushort[] {135, 137, 138, 139, 141},
			new ushort[] {142, 144, 145, 146, 148},
			new ushort[] {149, 151, 152, 153, 155},
			new ushort[] {156, 158, 159, 160, 162},
		};

		CPos previousPaintedCell;
		ushort previousPaintedTemplate = 0;
		bool painting;

		public EditorTileAutoappendBrush(EditorViewportControllerWidget editorWidget, ushort template, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			Template = template;
			worldRenderer = wr;
			world = wr.World;

			GenerateNewPreview();

			preview.Template = world.TileSet.Templates.First(t => t.Value.Id == template).Value;
			var grid = world.Map.Grid;
			bounds = worldRenderer.Theater.TemplateBounds(preview.Template, grid.TileSize, grid.Type);

			highlightLayer = wr.World.WorldActor.Trait<EditorHighlightLayer>();

			// The preview widget may be rendered by the higher-level code before it is ticked.
			// Force a manual tick to ensure the bounds are set correctly for this first draw.
			Tick();
		}

		void GenerateNewPreview()
		{
			preview = editorWidget.Get<TerrainTemplatePreviewWidget>("DRAG_TILE_PREVIEW");
			preview.GetScale = () => worldRenderer.Viewport.Zoom;
			preview.IsVisible = () => editorWidget.CurrentBrush == this;
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			// Exclusively uses left and right mouse buttons, but nothing else
			if (mi.Button != MouseButton.Left && mi.Button != MouseButton.Right)
				return false;

			if (mi.Button == MouseButton.Right)
			{
				if (mi.Event == MouseInputEvent.Up)
				{
					editorWidget.ClearBrush();
					return true;
				}

				return false;
			}

			if (mi.Button == MouseButton.Left)
			{
				if (mi.Event == MouseInputEvent.Down)
					painting = true;
				else if (mi.Event == MouseInputEvent.Up)
				{
					painting = false;
					previousPaintedCell = new CPos();
					previousPaintedTemplate = 0;
				}
			}

			if (!painting)
				return true;

			var map = world.Map;
			var mapTiles = map.MapTiles.Value;
			var mapHeight = map.MapHeight.Value;
			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);

			if (mi.Event != MouseInputEvent.Down && mi.Event != MouseInputEvent.Move)
				return true;

			var rules = map.Rules;
			var tileset = rules.TileSets[map.Tileset];
			var _template = Template;
			var similarTileset = similarTilesets.Where(t => t.Contains(_template)).FirstOrDefault();
			if (similarTileset != null && previousPaintedTemplate != 0)
			{
				var first = similarTileset.First();
				var last = similarTileset.Last();
				_template = similarTileset.Except(new ushort[] {first, last, previousPaintedTemplate}).Random(new Support.MersenneTwister());
			}
			else if (previousPaintedTemplate == 0)
			{
				_template = 151;
			}
			var template = tileset.Templates[_template];
			var baseHeight = mapHeight.Contains(cell) ? mapHeight[cell] : (byte)0;
			if (mi.Event == MouseInputEvent.Move && PlacementOverlapsSameTemplate(template, cell))
				return true;

			path.Add(preview);
			GenerateNewPreview();
			previousPaintedCell = cell;
			previousPaintedTemplate = _template;

			return true;
		}

		bool PlacementOverlapsSameTemplate(TerrainTemplateInfo template, CPos cell)
		{
			var map = world.Map;
			var mapTiles = map.MapTiles.Value;
			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++, i++)
				{
					if (template.Contains(i) && template[i] != null)
					{
						var c = cell + new CVec(x, y);

						if (mapTiles.Contains(c))
						{
							var otherTemplate = mapTiles[c].Type;
							var similarTileset = similarTilesets.Where(t => t.Contains(template.Id)).FirstOrDefault();
							if (otherTemplate == template.Id || (similarTileset != null && similarTileset.Contains(otherTemplate)))
								return true;
						}
					}
				}
			}

			return false;
		}

		public void Tick()
		{
			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);

			var offset = WVec.Zero;
			var location = world.Map.CenterOfCell(cell) + offset;

			var cellScreenPosition = worldRenderer.ScreenPxPosition(location);
			var cellScreenPixel = worldRenderer.Viewport.WorldToViewPx(cellScreenPosition);
			var zoom = worldRenderer.Viewport.Zoom;

			preview.Bounds.X = cellScreenPixel.X + (int)(zoom * bounds.X);
			preview.Bounds.Y = cellScreenPixel.Y + (int)(zoom * bounds.Y);
			preview.Bounds.Width = (int)(zoom * bounds.Width);
			preview.Bounds.Height = (int)(zoom * bounds.Height);
			//highlightLayer.SetHighlightRegion(cell, cell + new CVec(1, 1));
		}

		public void Dispose()
		{
			highlightLayer.Clear();
		}
	}
}
