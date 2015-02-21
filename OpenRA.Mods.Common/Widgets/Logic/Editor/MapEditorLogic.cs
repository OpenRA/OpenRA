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
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapEditorLogic
	{
		readonly Ruleset rules;

		CPos cachedMouseCursorCell;
		Rectangle draggedTemplateBounds;
		ResourceTypeInfo draggedResourceTypeInfo;
		string draggedActor;
		int cachedFacing;
		string cachedOwner;

		[ObjectCreator.UseCtor]
		public MapEditorLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			rules = world.Map.Rules;

			var mapEditorWidget = widget.Get<MapEditorWidget>("MAP_EDITOR");

			var dragTilePreview = widget.Get<TerrainTemplatePreviewWidget>("DRAG_TILE_PREVIEW");
			dragTilePreview.GetScale = () => worldRenderer.Viewport.Zoom;
			dragTilePreview.IsVisible = () => mapEditorWidget.CurrentPreview == MapEditorWidget.DragType.Tiles;

			var dragLayerPreview = widget.Get<SpriteWidget>("DRAG_LAYER_PREVIEW");
			dragLayerPreview.GetScale = () => worldRenderer.Viewport.Zoom;
			dragLayerPreview.IsVisible = () => mapEditorWidget.CurrentPreview == MapEditorWidget.DragType.Layers;
			dragLayerPreview.GetPalette = () => mapEditorWidget.SelectedResourceTypeInfo.Palette;
			dragLayerPreview.GetSprite = () =>
			{
				var resource = mapEditorWidget.SelectedResourceTypeInfo;
				var variant = resource.Variants.FirstOrDefault();
				var sequenceProvier = rules.Sequences[world.TileSet.Id];
				var sequence = sequenceProvier.GetSequence("resources", variant);
				var frame = sequence.Frames != null ? sequence.Frames.Last() : resource.MaxDensity - 1;
				return sequence.GetSprite(frame);
			};

			var dragActorPreview = widget.Get<ActorPreviewWidget>("DRAG_ACTOR_PREVIEW");
			dragActorPreview.GetScale = () => worldRenderer.Viewport.Zoom;
			dragActorPreview.IsVisible = () => mapEditorWidget.CurrentPreview == MapEditorWidget.DragType.Actors;

			var ticker = widget.Get<LogicTickerWidget>("EDITOR_TICKER");
			ticker.OnTick = () =>
			{
				if (mapEditorWidget.SelectedTileId.HasValue)
				{
					if (dragTilePreview.Template == null || mapEditorWidget.SelectedTileId != dragTilePreview.Template.Id)
					{
						dragTilePreview.Template = world.TileSet.Templates.First(t => t.Value.Id == mapEditorWidget.SelectedTileId).Value;
						draggedTemplateBounds = worldRenderer.Theater.TemplateBounds(dragTilePreview.Template, Game.ModData.Manifest.TileSize, world.Map.TileShape);
						mapEditorWidget.CurrentPreview = MapEditorWidget.DragType.Tiles;
					}
				}

				if (draggedResourceTypeInfo != mapEditorWidget.SelectedResourceTypeInfo)
				{
					draggedResourceTypeInfo = mapEditorWidget.SelectedResourceTypeInfo;
					if (draggedResourceTypeInfo != null)
						mapEditorWidget.CurrentPreview = MapEditorWidget.DragType.Layers;
				}

				if (draggedActor != mapEditorWidget.SelectedActor || cachedFacing != mapEditorWidget.ActorFacing || cachedOwner != mapEditorWidget.SelectedOwner)
				{
					draggedActor = mapEditorWidget.SelectedActor;
					if (draggedActor != null)
					{
						var actor = rules.Actors[draggedActor];
						cachedOwner = mapEditorWidget.SelectedOwner;
						var player = world.Players.FirstOrDefault(p => p.InternalName == mapEditorWidget.SelectedOwner) ?? world.Players.First();
						var typeDictionary = new TypeDictionary();
						typeDictionary.Add(new FacingInit(mapEditorWidget.ActorFacing));
						typeDictionary.Add(new TurretFacingInit(mapEditorWidget.ActorFacing));
						cachedFacing = mapEditorWidget.ActorFacing;
						dragActorPreview.SetPreview(actor, player, typeDictionary);
						mapEditorWidget.CurrentPreview = MapEditorWidget.DragType.Actors;
					}
				}

				if (cachedMouseCursorCell != mapEditorWidget.MouseCursorCell)
				{
					cachedMouseCursorCell = mapEditorWidget.MouseCursorCell;

					var cell = mapEditorWidget.MouseCursorCell;
					var offset = new WVec();

					if (!string.IsNullOrEmpty(draggedActor))
					{
						var traits = rules.Actors[draggedActor].Traits;
						var buildingInfo = traits.GetOrDefault<BuildingInfo>();
						if (buildingInfo != null)
						{
							cell -= FootprintUtils.AccountForBuidingSize(buildingInfo) / 2;
							offset = FootprintUtils.CenterOffset(world, buildingInfo);
						}
					}

					var location = world.Map.CenterOfCell(cell) + offset;

					var cellScreenPosition = worldRenderer.ScreenPxPosition(location);
					var cellScreenPixel = worldRenderer.Viewport.WorldToViewPx(cellScreenPosition);

					if (dragActorPreview.IsVisible())
					{
						var zoom = worldRenderer.Viewport.Zoom;
						var s = dragActorPreview.IdealPreviewSize;
						var o = dragActorPreview.PreviewOffset;
						dragActorPreview.Bounds.X = cellScreenPixel.X - (int)(zoom * (o.X + s.X / 2));
						dragActorPreview.Bounds.Y = cellScreenPixel.Y - (int)(zoom * (o.Y + s.Y / 2));
						dragActorPreview.Bounds.Width = (int)(zoom * s.X);
						dragActorPreview.Bounds.Height = (int)(zoom * s.Y);
					}

					if (dragTilePreview.IsVisible())
					{
						var zoom = worldRenderer.Viewport.Zoom;
						dragTilePreview.Bounds.X = cellScreenPixel.X + (int)(zoom * draggedTemplateBounds.X);
						dragTilePreview.Bounds.Y = cellScreenPixel.Y + (int)(zoom * draggedTemplateBounds.Y);
						dragTilePreview.Bounds.Width = (int)(zoom * draggedTemplateBounds.Width);
						dragTilePreview.Bounds.Height = (int)(zoom * draggedTemplateBounds.Height);
					}

					if (dragLayerPreview.IsVisible())
					{
						dragLayerPreview.Bounds.X = cellScreenPixel.X;
						dragLayerPreview.Bounds.Y = cellScreenPixel.Y;
					}
				}
			};

			var gridButton = widget.Get<ButtonWidget>("GRID_BUTTON");
			var devTrait = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();
			gridButton.OnClick = () => devTrait.ShowTerrainGeometry ^= true;
			gridButton.IsHighlighted = () => devTrait.ShowTerrainGeometry;
		}
	}
}
