#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorResourceBrush : IEditorBrush
	{
		public readonly ResourceTypeInfo ResourceType;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorViewportControllerWidget editorWidget;
		readonly SpriteWidget preview;
		readonly EditorActionManager editorActionManager;

		AddResourcesEditorAction action;
		bool resourceAdded;

		public EditorResourceBrush(EditorViewportControllerWidget editorWidget, ResourceTypeInfo resource, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			ResourceType = resource;
			worldRenderer = wr;
			world = wr.World;
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			action = new AddResourcesEditorAction(world.Map, ResourceType);

			preview = editorWidget.Get<SpriteWidget>("DRAG_LAYER_PREVIEW");
			preview.Palette = resource.Palette;
			preview.GetScale = () => worldRenderer.Viewport.Zoom;
			preview.IsVisible = () => editorWidget.CurrentBrush == this;

			var variant = resource.Sequences.FirstOrDefault();
			var sequence = wr.World.Map.Rules.Sequences.GetSequence("resources", variant);
			var sprite = sequence.GetSprite(resource.MaxDensity - 1);
			preview.GetSprite = () => sprite;

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

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);

			if (mi.Button == MouseButton.Left && mi.Event != MouseInputEvent.Up && AllowResourceAt(cell))
			{
				var type = (byte)ResourceType.ResourceType;
				var index = (byte)ResourceType.MaxDensity;
				action.Add(new CellResource(cell, world.Map.Resources[cell], new ResourceTile(type, index)));
				resourceAdded = true;
			}
			else if (resourceAdded && mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				editorActionManager.Add(action);
				action = new AddResourcesEditorAction(world.Map, ResourceType);
				resourceAdded = false;
			}

			return true;
		}

		public bool AllowResourceAt(CPos cell)
		{
			var mapResources = world.Map.Resources;
			if (!mapResources.Contains(cell))
				return false;

			var tile = world.Map.Tiles[cell];
			var tileInfo = world.Map.Rules.TileSet.GetTileInfo(tile);
			if (tileInfo == null)
				return false;

			var terrainType = world.Map.Rules.TileSet.TerrainInfo[tileInfo.TerrainType];

			if (mapResources[cell].Type == ResourceType.ResourceType)
				return false;

			if (!ResourceType.AllowedTerrainTypes.Contains(terrainType.Type))
				return false;

			return ResourceType.AllowOnRamps || tileInfo.RampType == 0;
		}

		public void Tick()
		{
			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			var offset = WVec.Zero;
			var location = world.Map.CenterOfCell(cell) + offset;

			var cellScreenPosition = worldRenderer.ScreenPxPosition(location);
			var cellScreenPixel = worldRenderer.Viewport.WorldToViewPx(cellScreenPosition);

			preview.Bounds.X = cellScreenPixel.X;
			preview.Bounds.Y = cellScreenPixel.Y;
		}

		public void Dispose() { }
	}

	struct CellResource
	{
		public readonly CPos Cell;
		public readonly ResourceTile ResourceTile;
		public readonly ResourceTile NewResourceTile;

		public CellResource(CPos cell, ResourceTile resourceTile, ResourceTile newResourceTile)
		{
			Cell = cell;
			ResourceTile = resourceTile;
			NewResourceTile = newResourceTile;
		}
	}

	class AddResourcesEditorAction : IEditorAction
	{
		public string Text { get; private set; }

		readonly Map map;
		readonly ResourceTypeInfo resourceType;
		readonly List<CellResource> cellResources = new List<CellResource>();

		public AddResourcesEditorAction(Map map, ResourceTypeInfo resourceType)
		{
			this.map = map;
			this.resourceType = resourceType;
		}

		public void Execute()
		{
		}

		public void Do()
		{
			foreach (var resourceCell in cellResources)
				SetTile(resourceCell.Cell, resourceCell.NewResourceTile);
		}

		void SetTile(CPos cell, ResourceTile tile)
		{
			map.Resources[cell] = tile;
		}

		public void Undo()
		{
			foreach (var resourceCell in cellResources)
				SetTile(resourceCell.Cell, resourceCell.ResourceTile);
		}

		public void Add(CellResource cellResource)
		{
			SetTile(cellResource.Cell, cellResource.NewResourceTile);
			cellResources.Add(cellResource);

			var cellText = cellResources.Count != 1 ? "cells" : "cell";
			Text = "Added {0} {1} of {2}".F(cellResources.Count, cellText, resourceType.TerrainType);
		}
	}
}
