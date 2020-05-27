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
	[Flags]
	public enum MapCopyFilters
	{
		None = 0,
		Terrain = 1,
		Resources = 2,
		Actors = 4,
		All = Terrain | Resources | Actors
	}

	public sealed class EditorCopyPasteBrush : IEditorBrush
	{
		enum State { SelectFirst, SelectSecond, Paste }

		readonly WorldRenderer worldRenderer;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorSelectionLayer selectionLayer;
		readonly EditorActorLayer editorLayer;
		readonly Func<MapCopyFilters> getCopyFilters;
		readonly EditorActionManager editorActionManager;

		State state;
		CPos start;
		CPos end;

		public EditorCopyPasteBrush(EditorViewportControllerWidget editorWidget, WorldRenderer wr, Func<MapCopyFilters> getCopyFilters)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;

			editorActionManager = wr.World.WorldActor.Trait<EditorActionManager>();

			selectionLayer = wr.World.WorldActor.Trait<EditorSelectionLayer>();
			editorLayer = wr.World.WorldActor.Trait<EditorActorLayer>();
			this.getCopyFilters = getCopyFilters;
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

			if (mi.Button == MouseButton.Left && (mi.Event == MouseInputEvent.Up || mi.Event == MouseInputEvent.Down))
			{
				var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);
				switch (state)
				{
					case State.SelectFirst:
						if (mi.Event != MouseInputEvent.Down)
							break;
						start = cell;
						selectionLayer.SetCopyRegion(start, end);
						state = State.SelectSecond;
						break;
					case State.SelectSecond:
						if (mi.Event != MouseInputEvent.Up)
							break;
						end = cell;
						selectionLayer.SetCopyRegion(start, end);
						state = State.Paste;
						break;
					case State.Paste:
					{
						var gridType = worldRenderer.World.Map.Grid.Type;
						var source = CellRegion.BoundingRegion(gridType, new[] { start, end });
						Copy(source, cell - end);
						editorWidget.ClearBrush();
						break;
					}
				}

				return true;
			}

			return false;
		}

		void Copy(CellRegion source, CVec offset)
		{
			var gridType = worldRenderer.World.Map.Grid.Type;
			var mapTiles = worldRenderer.World.Map.Tiles;
			var mapHeight = worldRenderer.World.Map.Height;
			var mapResources = worldRenderer.World.Map.Resources;

			var dest = new CellRegion(gridType, source.TopLeft + offset, source.BottomRight + offset);

			var previews = new Dictionary<string, ActorReference>();
			var tiles = new Dictionary<CPos, Tuple<TerrainTile, ResourceTile, byte>>();
			var copyFilters = getCopyFilters();

			foreach (var cell in source)
			{
				if (!mapTiles.Contains(cell) || !mapTiles.Contains(cell + offset))
					continue;

				tiles.Add(cell + offset, Tuple.Create(mapTiles[cell], mapResources[cell], mapHeight[cell]));

				if (copyFilters.HasFlag(MapCopyFilters.Actors))
				{
					foreach (var preview in editorLayer.PreviewsAt(cell))
					{
						if (previews.ContainsKey(preview.ID))
							continue;

						var copy = preview.Export();
						if (copy.InitDict.Contains<LocationInit>())
						{
							var location = copy.InitDict.Get<LocationInit>();
							copy.InitDict.Remove(location);
							copy.InitDict.Add(new LocationInit(location.Value + offset));
						}

						previews.Add(preview.ID, copy);
					}
				}
			}

			var action = new CopyPasteEditorAction(copyFilters, worldRenderer.World.Map, tiles, previews, editorLayer, dest);
			editorActionManager.Add(action);
		}

		public void Tick()
		{
			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (state == State.Paste)
			{
				selectionLayer.SetPasteRegion(cell + (start - end), cell);
				return;
			}

			if (state == State.SelectFirst)
				start = end = cell;
			else if (state == State.SelectSecond)
				end = cell;

			selectionLayer.SetCopyRegion(start, end);
		}

		public void Dispose()
		{
			selectionLayer.Clear();
		}
	}

	class CopyPasteEditorAction : IEditorAction
	{
		public string Text { get; private set; }

		readonly MapCopyFilters copyFilters;
		readonly Dictionary<CPos, Tuple<TerrainTile, ResourceTile, byte>> tiles;
		readonly Dictionary<string, ActorReference> previews;
		readonly EditorActorLayer editorLayer;
		readonly CellRegion dest;
		readonly CellLayer<TerrainTile> mapTiles;
		readonly CellLayer<byte> mapHeight;
		readonly CellLayer<ResourceTile> mapResources;

		readonly Queue<UndoCopyPaste> undoCopyPastes = new Queue<UndoCopyPaste>();
		readonly Queue<EditorActorPreview> removedActors = new Queue<EditorActorPreview>();
		readonly Queue<EditorActorPreview> addedActorPreviews = new Queue<EditorActorPreview>();

		public CopyPasteEditorAction(MapCopyFilters copyFilters, Map map,
			Dictionary<CPos, Tuple<TerrainTile, ResourceTile, byte>> tiles, Dictionary<string, ActorReference> previews,
			EditorActorLayer editorLayer, CellRegion dest)
		{
			this.copyFilters = copyFilters;
			this.tiles = tiles;
			this.previews = previews;
			this.editorLayer = editorLayer;
			this.dest = dest;

			mapTiles = map.Tiles;
			mapHeight = map.Height;
			mapResources = map.Resources;

			Text = "Copied {0} tiles".F(tiles.Count);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			foreach (var kv in tiles)
			{
				undoCopyPastes.Enqueue(new UndoCopyPaste(kv.Key, mapTiles[kv.Key], mapResources[kv.Key], mapHeight[kv.Key]));

				if (copyFilters.HasFlag(MapCopyFilters.Terrain))
					mapTiles[kv.Key] = kv.Value.Item1;

				if (copyFilters.HasFlag(MapCopyFilters.Resources))
					mapResources[kv.Key] = kv.Value.Item2;

				mapHeight[kv.Key] = kv.Value.Item3;
			}

			if (copyFilters.HasFlag(MapCopyFilters.Actors))
			{
				var removeActors = dest.SelectMany(editorLayer.PreviewsAt).Distinct().ToList();
				foreach (var preview in removeActors)
				{
					removedActors.Enqueue(preview);
					editorLayer.Remove(preview);
				}
			}

			foreach (var kv in previews)
				addedActorPreviews.Enqueue(editorLayer.Add(kv.Value));
		}

		public void Undo()
		{
			while (undoCopyPastes.Count > 0)
			{
				var undoCopyPaste = undoCopyPastes.Dequeue();

				var cell = undoCopyPaste.Cell;

				if (copyFilters.HasFlag(MapCopyFilters.Terrain))
					mapTiles[cell] = undoCopyPaste.MapTile;

				if (copyFilters.HasFlag(MapCopyFilters.Resources))
					mapResources[cell] = undoCopyPaste.ResourceTile;

				mapHeight[cell] = undoCopyPaste.Height;
			}

			while (addedActorPreviews.Count > 0)
				editorLayer.Remove(addedActorPreviews.Dequeue());

			if (copyFilters.HasFlag(MapCopyFilters.Actors))
				while (removedActors.Count > 0)
					editorLayer.Add(removedActors.Dequeue());
		}
	}

	internal class UndoCopyPaste
	{
		public CPos Cell { get; private set; }
		public TerrainTile MapTile { get; private set; }
		public ResourceTile ResourceTile { get; private set; }
		public byte Height { get; private set; }

		public UndoCopyPaste(CPos cell, TerrainTile mapTile, ResourceTile resourceTile, byte height)
		{
			Cell = cell;
			MapTile = mapTile;
			ResourceTile = resourceTile;
			Height = height;
		}
	}
}
