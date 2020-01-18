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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorTileBrush : IEditorBrush
	{
		public readonly ushort Template;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorActionManager editorActionManager;
		readonly EditorCursorLayer editorCursor;
		readonly int cursorToken;

		bool painting;

		public EditorTileBrush(EditorViewportControllerWidget editorWidget, ushort id, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			editorCursor = world.WorldActor.Trait<EditorCursorLayer>();

			Template = id;
			worldRenderer = wr;
			world = wr.World;

			var template = world.Map.Rules.TileSet.Templates.First(t => t.Value.Id == id).Value;
			cursorToken = editorCursor.SetTerrainTemplate(wr, template);
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

			if (mi.Event != MouseInputEvent.Down && mi.Event != MouseInputEvent.Move)
				return true;

			if (editorCursor.CurrentToken != cursorToken)
				return false;

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);
			var isMoving = mi.Event == MouseInputEvent.Move;

			if (mi.Modifiers.HasModifier(Modifiers.Shift))
			{
				FloodFillWithBrush(cell, isMoving);
				painting = false;
			}
			else
				PaintCell(cell, isMoving);

			return true;
		}

		void PaintCell(CPos cell, bool isMoving)
		{
			var map = world.Map;
			var tileset = map.Rules.TileSet;
			var template = tileset.Templates[Template];

			if (isMoving && PlacementOverlapsSameTemplate(template, cell))
				return;

			editorActionManager.Add(new PaintTileEditorAction(Template, map, cell));
		}

		void FloodFillWithBrush(CPos cell, bool isMoving)
		{
			var map = world.Map;
			var mapTiles = map.Tiles;
			var replace = mapTiles[cell];

			if (replace.Type == Template)
				return;

			var queue = new Queue<CPos>();
			var touched = new CellLayer<bool>(map);

			var tileset = map.Rules.TileSet;
			var template = tileset.Templates[Template];

			Action<CPos> maybeEnqueue = newCell =>
			{
				if (map.Contains(cell) && !touched[newCell])
				{
					queue.Enqueue(newCell);
					touched[newCell] = true;
				}
			};

			Func<CPos, bool> shouldPaint = cellToCheck =>
			{
				for (var y = 0; y < template.Size.Y; y++)
				{
					for (var x = 0; x < template.Size.X; x++)
					{
						var c = cellToCheck + new CVec(x, y);
						if (!map.Contains(c) || mapTiles[c].Type != replace.Type)
							return false;
					}
				}

				return true;
			};

			Func<CPos, CVec, CPos> findEdge = (refCell, direction) =>
			{
				while (true)
				{
					var newCell = refCell + direction;
					if (!shouldPaint(newCell))
						return refCell;
					refCell = newCell;
				}
			};

			queue.Enqueue(cell);
			while (queue.Count > 0)
			{
				var queuedCell = queue.Dequeue();
				if (!shouldPaint(queuedCell))
					continue;

				var previousCell = findEdge(queuedCell, new CVec(-1 * template.Size.X, 0));
				var nextCell = findEdge(queuedCell, new CVec(1 * template.Size.X, 0));

				for (var x = previousCell.X; x <= nextCell.X; x += template.Size.X)
				{
					PaintCell(new CPos(x, queuedCell.Y), isMoving);
					var upperCell = new CPos(x, queuedCell.Y - (1 * template.Size.Y));
					var lowerCell = new CPos(x, queuedCell.Y + (1 * template.Size.Y));

					if (shouldPaint(upperCell))
						maybeEnqueue(upperCell);
					if (shouldPaint(lowerCell))
						maybeEnqueue(lowerCell);
				}
			}
		}

		bool PlacementOverlapsSameTemplate(TerrainTemplateInfo template, CPos cell)
		{
			var map = world.Map;
			var mapTiles = map.Tiles;
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

		public void Tick() { }

		public void Dispose()
		{
			editorCursor.Clear(cursorToken);
		}
	}

	class PaintTileEditorAction : IEditorAction
	{
		public string Text { get; private set; }

		readonly ushort template;
		readonly Map map;
		readonly CPos cell;

		readonly Queue<UndoTile> undoTiles = new Queue<UndoTile>();
		readonly TerrainTemplateInfo terrainTemplate;

		public PaintTileEditorAction(ushort template, Map map, CPos cell)
		{
			this.template = template;
			this.map = map;
			this.cell = cell;

			var tileset = map.Rules.TileSet;
			terrainTemplate = tileset.Templates[template];
			Text = "Added tile {0}".F(terrainTemplate.Id);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(cell) ? mapHeight[cell] : (byte)0;

			var i = 0;
			for (var y = 0; y < terrainTemplate.Size.Y; y++)
			{
				for (var x = 0; x < terrainTemplate.Size.X; x++, i++)
				{
					if (terrainTemplate.Contains(i) && terrainTemplate[i] != null)
					{
						var index = terrainTemplate.PickAny ? (byte)Game.CosmeticRandom.Next(0, terrainTemplate.TilesCount) : (byte)i;
						var c = cell + new CVec(x, y);
						if (!mapTiles.Contains(c))
							continue;

						undoTiles.Enqueue(new UndoTile(c, mapTiles[c], mapHeight[c]));

						mapTiles[c] = new TerrainTile(template, index);
						mapHeight[c] = (byte)(baseHeight + terrainTemplate[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
					}
				}
			}
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();

				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}
	}

	class UndoTile
	{
		public CPos Cell { get; private set; }
		public TerrainTile MapTile { get; private set; }
		public byte Height { get; private set; }

		public UndoTile(CPos cell, TerrainTile mapTile, byte height)
		{
			Cell = cell;
			MapTile = mapTile;
			Height = height;
		}
	}
}
