#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Widgets;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorEyedropperTileBrush : IEditorBrush
	{
		enum State { SelectFirst, SelectSecond, Paste }

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorHighlightLayer highlightLayer;

		public EditorEyedropperTileBrush(EditorViewportControllerWidget editorWidget, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;

			highlightLayer = wr.World.WorldActor.Trait<EditorHighlightLayer>();
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
			{
				if (mi.Event == MouseInputEvent.Up)
				{
					editorWidget.ClearBrush();
					return true;
				}

				return false;
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up && editorWidget.IsEyedropping)
			{
				// Handle eydropper tool - interacts with the tile selector logic
				var map = world.Map;
				var mapTiles = map.MapTiles.Value;
				var mapHeight = map.MapHeight.Value;

				var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);

				var tileset = map.Rules.TileSets[worldRenderer.World.Map.Tileset];

				var template = mapTiles[cell].Type;
				var category = tileset.Templates[template].Category;
				var tileSelectorWidget = editorWidget.Parent.Get<ContainerWidget>("TILE_WIDGETS");

				var logic = (Logic.TileSelectorLogic)tileSelectorWidget.LogicObjects[0];
				editorWidget.SetBrush(new EditorTileBrush(editorWidget, template, worldRenderer));
				logic.SwitchCategory(worldRenderer, tileset, category);

				return true;
			}

			return false;
		}

		public void Tick()
		{
			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);

			highlightLayer.SetHighlightRegion(cell, cell);
		}

		public void Dispose()
		{
			highlightLayer.Clear();
			if (editorWidget.IsEyedropping)
			{
				editorWidget.EndEyedropping();
			}
		}
	}
}
