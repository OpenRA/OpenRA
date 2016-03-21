#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	public sealed class EditorCopyPasteBrush : IEditorBrush
	{
		enum State { SelectFirst, SelectSecond, Paste }

		readonly WorldRenderer worldRenderer;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorSelectionLayer selectionLayer;
		readonly EditorActorLayer editorLayer;

		State state;
		CPos start;
		CPos end;

		public EditorCopyPasteBrush(EditorViewportControllerWidget editorWidget, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;

			selectionLayer = wr.World.WorldActor.Trait<EditorSelectionLayer>();
			editorLayer = wr.World.WorldActor.Trait<EditorActorLayer>();
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

			foreach (var cell in source)
			{
				if (!mapTiles.Contains(cell) || !mapTiles.Contains(cell + offset))
					continue;

				tiles.Add(cell + offset, Tuple.Create(mapTiles[cell], mapResources[cell], mapHeight[cell]));

				foreach (var preview in editorLayer.PreviewsAt(cell))
				{
					if (previews.ContainsKey(preview.ID))
						continue;

					var copy = preview.Export();
					if (copy.InitDict.Contains<LocationInit>())
					{
						var location = copy.InitDict.Get<LocationInit>();
						copy.InitDict.Remove(location);
						copy.InitDict.Add(new LocationInit(location.Value(worldRenderer.World) + offset));
					}

					previews.Add(preview.ID, copy);
				}
			}

			foreach (var kv in tiles)
			{
				mapTiles[kv.Key] = kv.Value.Item1;
				mapResources[kv.Key] = kv.Value.Item2;
				mapHeight[kv.Key] = kv.Value.Item3;
			}

			var removeActors = dest.SelectMany(editorLayer.PreviewsAt).Distinct().ToList();
			foreach (var preview in removeActors)
				editorLayer.Remove(preview);

			foreach (var kv in previews)
				editorLayer.Add(kv.Value);
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
}
