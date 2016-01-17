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

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorTileBrush : IEditorBrush
	{
		public readonly ushort Template;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorViewportControllerWidget editorWidget;
		readonly TerrainTemplatePreviewWidget preview;
		readonly Rectangle bounds;

		bool painting;

		public EditorTileBrush(EditorViewportControllerWidget editorWidget, ushort template, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			Template = template;
			worldRenderer = wr;
			world = wr.World;

			preview = editorWidget.Get<TerrainTemplatePreviewWidget>("DRAG_TILE_PREVIEW");
			preview.GetScale = () => worldRenderer.Viewport.Zoom;
			preview.IsVisible = () => editorWidget.CurrentBrush == this;

			preview.Template = world.TileSet.Templates.First(t => t.Value.Id == template).Value;
			var grid = world.Map.Grid;
			bounds = worldRenderer.Theater.TemplateBounds(preview.Template, grid.TileSize, grid.Type);

			// The preview widget may be rendered by the higher-level code before it is ticked.
			// Force a manual tick to ensure the bounds are set correctly for this first draw.
			Tick();
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
					painting = false;
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
			var template = tileset.Templates[Template];
			var baseHeight = mapHeight.Contains(cell) ? mapHeight[cell] : (byte)0;
			if (mi.Event == MouseInputEvent.Move && PlacementOverlapsSameTemplate(template, cell))
				return true;

			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++, i++)
				{
					if (template.Contains(i) && template[i] != null)
					{
						var index = template.PickAny ? (byte)Game.CosmeticRandom.Next(0, template.TilesCount) : (byte)i;
						var c = cell + new CVec(x, y);
						if (!mapTiles.Contains(c))
							continue;

						mapTiles[c] = new TerrainTile(Template, index);
						mapHeight[c] = (byte)(baseHeight + template[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
					}
				}
			}

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
						if (mapTiles.Contains(c) && mapTiles[c].Type == template.Id)
							return true;
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
		}

		public void Dispose() { }
	}
}
