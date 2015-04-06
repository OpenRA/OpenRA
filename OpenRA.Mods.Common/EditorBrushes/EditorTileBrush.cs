#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorTileBrush : IEditorBrush
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
			bounds = worldRenderer.Theater.TemplateBounds(preview.Template, Game.ModData.Manifest.TileSize, world.Map.TileShape);

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
				editorWidget.ClearBrush();
				return true;
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
			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);

			if (mi.Event != MouseInputEvent.Down && mi.Event != MouseInputEvent.Move)
				return true;

			var rules = map.Rules;
			var tileset = rules.TileSets[map.Tileset];
			var template = tileset.Templates[Template];
			var baseHeight = map.Contains(cell) ? map.MapHeight.Value[cell] : 0;
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
						if (!map.Contains(c))
							continue;

						map.MapTiles.Value[c] = new TerrainTile(Template, index);
						map.MapHeight.Value[c] = (byte)(baseHeight + template[index].Height).Clamp(0, world.TileSet.MaxGroundHeight);
					}
				}
			}

			return true;
		}

		bool PlacementOverlapsSameTemplate(TerrainTemplateInfo template, CPos cell)
		{
			var map = world.Map;
			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++, i++)
				{
					if (template.Contains(i) && template[i] != null)
					{
						var c = cell + new CVec(x, y);
						if (map.Contains(c) && map.MapTiles.Value[c].Type == template.Id)
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
	}
}
