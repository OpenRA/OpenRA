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
	public class EditorResourceBrush : IEditorBrush
	{
		public readonly ResourceTypeInfo ResourceType;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorViewportControllerWidget editorWidget;
		readonly SpriteWidget preview;

		public EditorResourceBrush(EditorViewportControllerWidget editorWidget, ResourceTypeInfo resource, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			ResourceType = resource;
			worldRenderer = wr;
			world = wr.World;

			preview = editorWidget.Get<SpriteWidget>("DRAG_LAYER_PREVIEW");
			preview.Palette = resource.Palette;
			preview.GetScale = () => worldRenderer.Viewport.Zoom;
			preview.IsVisible = () => editorWidget.CurrentBrush == this;

			var variant = resource.Variants.FirstOrDefault();
			var sequenceProvider = wr.World.Map.Rules.Sequences[world.TileSet.Id];
			var sequence = sequenceProvider.GetSequence("resources", variant);
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
				editorWidget.ClearBrush();
				return true;
			}

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);

			if (mi.Button == MouseButton.Left && AllowResourceAt(cell))
			{
				var type = (byte)ResourceType.ResourceType;
				var index = (byte)ResourceType.MaxDensity;
				world.Map.MapResources.Value[cell] = new ResourceTile(type, index);
			}

			return true;
		}

		public bool AllowResourceAt(CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			var tile = world.Map.MapTiles.Value[cell];
			var tileInfo = world.TileSet.GetTileInfo(tile);
			var terrainType = world.TileSet.TerrainInfo[tileInfo.TerrainType];

			if (world.Map.MapResources.Value[cell].Type == ResourceType.ResourceType)
				return false;

			if (!ResourceType.AllowedTerrainTypes.Contains(terrainType.Type))
				return false;

			return ResourceType.AllowOnRamps || tileInfo == null || tileInfo.RampType == 0;
		}

		public void Tick()
		{
			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			var offset = WVec.Zero;
			var location = world.Map.CenterOfCell(cell) + offset;

			var cellScreenPosition = worldRenderer.ScreenPxPosition(location);
			var cellScreenPixel = worldRenderer.Viewport.WorldToViewPx(cellScreenPosition);
			var zoom = worldRenderer.Viewport.Zoom;

			preview.Bounds.X = cellScreenPixel.X;
			preview.Bounds.Y = cellScreenPixel.Y;
		}
	}
}
