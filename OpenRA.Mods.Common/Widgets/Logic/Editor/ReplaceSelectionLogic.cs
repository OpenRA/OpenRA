#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[IncludeStaticFluentReferences(
		typeof(ReplaceSelectionEditorAction),
		typeof(ReplaceTemplatePlacementsEditorAction))]
	public sealed class ReplaceSelectionLogic : ChromeLogic
	{
		[FluentReference]
		const string Title = "label-editor-replace-title";

		[FluentReference]
		const string AssetTile = "label-editor-replace-asset-tile";

		[FluentReference("count")]
		const string AssetTiles = "label-editor-replace-asset-tiles";

		[FluentReference]
		const string AssetResource = "label-editor-replace-asset-resource";

		[FluentReference]
		const string AssetActor = "label-editor-replace-asset-actor";

		[FluentReference("count")]
		const string AssetActors = "label-editor-replace-asset-actors";

		[FluentReference]
		const string AssetMissing = "label-editor-replace-asset-missing";

		[FluentReference]
		const string LayerMismatch = "label-editor-replace-layer-mismatch";

		[FluentReference]
		const string NoSelection = "label-editor-replace-no-selection";

		[FluentReference]
		const string NoReplaceLayers = "label-editor-replace-no-replace-layers";

		[FluentReference]
		const string NothingToReplace = "label-editor-replace-nothing-to-replace";

		readonly EditorViewportControllerWidget editor;
		readonly World world;
		readonly Map map;
		readonly EditorActionManager editorActionManager;
		readonly EditorActorLayer editorActorLayer;
		readonly IResourceLayer resourceLayer;
		readonly LabelWidget assetLabel;

		bool replaceTile = true;
		bool replaceResources;
		bool replaceActors;
		bool includeEmptySpaces;
		EditorReplaceLayer withLayer = EditorReplaceLayer.Tile;

		[ObjectCreator.UseCtor]
		public ReplaceSelectionLogic(Widget widget, World world)
		{
			this.world = world;
			map = world.Map;
			editor = Ui.Root.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();

			widget.Get<LabelWidget>("TITLE").GetText = () => FluentProvider.GetMessage(Title);

			BindReplaceCheckbox(widget, "REPLACE_TILE_CHECKBOX", () => replaceTile, v => replaceTile = v);
			BindReplaceCheckbox(widget, "REPLACE_RESOURCES_CHECKBOX", () => replaceResources, v => replaceResources = v);
			BindReplaceCheckbox(widget, "REPLACE_ACTORS_CHECKBOX", () => replaceActors, v => replaceActors = v);

			BindWithCheckbox(widget, "WITH_TILE_CHECKBOX", EditorReplaceLayer.Tile);
			BindWithCheckbox(widget, "WITH_RESOURCES_CHECKBOX", EditorReplaceLayer.Resources);
			BindWithCheckbox(widget, "WITH_ACTORS_CHECKBOX", EditorReplaceLayer.Actors);

			var includeEmptyCheckbox = widget.Get<CheckboxWidget>("INCLUDE_EMPTY_CHECKBOX");
			includeEmptyCheckbox.GetText = () => FluentProvider.GetMessage("label-editor-replace-include-empty");
			includeEmptyCheckbox.IsChecked = () => includeEmptySpaces;
			includeEmptyCheckbox.OnClick = () => includeEmptySpaces ^= true;

			assetLabel = widget.Get<LabelWidget>("ASSET_LABEL");
			assetLabel.GetText = GetAssetDescription;

			var confirmButton = widget.Get<ButtonWidget>("CONFIRM_BUTTON");
			confirmButton.GetText = () => FluentProvider.GetMessage("button-editor-replace-confirm");
			confirmButton.IsDisabled = () => !TryBuildAction(out _);
			confirmButton.OnClick = ConfirmReplace;

			var cancelButton = widget.Get<ButtonWidget>("CANCEL_BUTTON");
			cancelButton.OnClick = Ui.CloseWindow;
		}

		void BindReplaceCheckbox(Widget widget, string id, System.Func<bool> getter, System.Action<bool> setter)
		{
			var checkbox = widget.Get<CheckboxWidget>(id);
			checkbox.GetText = () => FluentProvider.GetMessage(LayerLabelKeyFromId(id));
			checkbox.IsChecked = getter;
			checkbox.OnClick = () => setter(!getter());
		}

		void BindWithCheckbox(Widget widget, string id, EditorReplaceLayer layer)
		{
			var checkbox = widget.Get<CheckboxWidget>(id);
			checkbox.GetText = () => FluentProvider.GetMessage(LayerLabelKey(layer));
			checkbox.IsChecked = () => withLayer == layer;
			checkbox.OnClick = () => withLayer = layer;
		}

		static string LayerLabelKeyFromId(string id)
		{
			if (id.Contains("TILE"))
				return "label-editor-replace-layer-tile";
			if (id.Contains("RESOURCES"))
				return "label-editor-replace-layer-resources";
			return "label-editor-replace-layer-actors";
		}

		MapBlitFilters GetReplaceFilters()
		{
			var filters = MapBlitFilters.None;
			if (replaceTile)
				filters |= MapBlitFilters.Terrain;
			if (replaceResources)
				filters |= MapBlitFilters.Resources;
			if (replaceActors)
				filters |= MapBlitFilters.Actors;
			return filters;
		}

		string GetAssetDescription()
		{
			if (!editor.DefaultBrush.Selection.Area.HasValue)
				return FluentProvider.GetMessage(NoSelection);

			if (GetReplaceFilters() == MapBlitFilters.None)
				return FluentProvider.GetMessage(NoReplaceLayers);

			if (!TryGetWithAssets(out _))
				return FluentProvider.GetMessage(LayerMismatch, "layer", FluentProvider.GetMessage(LayerLabelKey(withLayer)));

			var selection = editor.DefaultBrush.Selection;
			if (!ReplaceSelectionEditorAction.HasReplaceableContent(
				GetReplaceFilters(), withLayer, includeEmptySpaces, map,
				selection.Area.Value, selection.GetAreaMask(), resourceLayer, editorActorLayer))
				return FluentProvider.GetMessage(NothingToReplace);

			return DescribeWithAssets();
		}

		string DescribeWithAssets()
		{
			switch (withLayer)
			{
				case EditorReplaceLayer.Tile when editor.CurrentBrush is EditorTileBrush tileBrush:
					return tileBrush.Templates.Length == 1
						? FluentProvider.GetMessage(AssetTile, "id", tileBrush.TerrainTemplate.Id)
						: FluentProvider.GetMessage(AssetTiles, "count", tileBrush.Templates.Length);
				case EditorReplaceLayer.Resources when editor.CurrentBrush is EditorResourceBrush resourceBrush:
					return FluentProvider.GetMessage(AssetResource, "type", resourceBrush.ResourceType);
				case EditorReplaceLayer.Actors when editor.CurrentBrush is EditorActorBrush actorBrush:
					return actorBrush.Actors.Length == 1
						? FluentProvider.GetMessage(AssetActor, "name", actorBrush.Actors[0].Name)
						: FluentProvider.GetMessage(AssetActors, "count", actorBrush.Actors.Length);
				default:
					return FluentProvider.GetMessage(AssetMissing, "layer", FluentProvider.GetMessage(LayerLabelKey(withLayer)));
			}
		}

		static string LayerLabelKey(EditorReplaceLayer replaceLayer)
		{
			return replaceLayer switch
			{
				EditorReplaceLayer.Tile => "label-editor-replace-layer-tile",
				EditorReplaceLayer.Resources => "label-editor-replace-layer-resources",
				_ => "label-editor-replace-layer-actors"
			};
		}

		bool TryGetWithAssets(out ReplaceWithAssets assets)
		{
			assets = default;
			switch (withLayer)
			{
				case EditorReplaceLayer.Tile when editor.CurrentBrush is EditorTileBrush tileBrush && tileBrush.Templates.Length > 0:
					assets = new ReplaceWithAssets { TileTemplates = tileBrush.Templates };
					return true;
				case EditorReplaceLayer.Resources when editor.CurrentBrush is EditorResourceBrush resourceBrush && resourceLayer != null:
					assets = new ReplaceWithAssets { ResourceTypes = [resourceBrush.ResourceType] };
					return true;
				case EditorReplaceLayer.Actors when editor.CurrentBrush is EditorActorBrush actorBrush && actorBrush.Actors.Length > 0:
					assets = new ReplaceWithAssets { ActorReferences = actorBrush.ActorReferences.ToArray() };
					return true;
				default:
					return false;
			}
		}

		bool TryBuildAction(out ReplaceSelectionEditorAction action)
		{
			action = null;
			var selection = editor.DefaultBrush.Selection;
			var replaceFilters = GetReplaceFilters();
			if (!selection.Area.HasValue || replaceFilters == MapBlitFilters.None || !TryGetWithAssets(out var assets))
				return false;

			action = ReplaceSelectionEditorAction.Create(
				replaceFilters,
				withLayer,
				assets.TileTemplates,
				assets.ResourceTypes,
				assets.ActorReferences,
				editor.AssetMixMode,
				includeEmptySpaces,
				map,
				selection.Area.Value,
				selection.GetAreaMask(),
				resourceLayer,
				editorActorLayer);

			return action != null;
		}

		void ConfirmReplace()
		{
			if (!TryBuildAction(out var action))
				return;

			editorActionManager.Add(action);
			Ui.CloseWindow();
		}

		struct ReplaceWithAssets
		{
			public ushort[] TileTemplates;
			public string[] ResourceTypes;
			public ActorReference[] ActorReferences;
		}
	}
}
