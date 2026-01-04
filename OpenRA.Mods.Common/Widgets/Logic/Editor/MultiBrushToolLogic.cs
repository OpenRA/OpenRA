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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[IncludeStaticFluentReferences(
		typeof(MultiBrushToolInfo),
		typeof(PaintBrushEditorAction))]
	public sealed class MultiBrushToolLogic : ChromeLogic
	{
		readonly ScrollPanelWidget widget;
		readonly EditorViewportControllerWidget editor;
		readonly MultiBrushTool tool;
		readonly WorldRenderer worldRenderer;

		[ObjectCreator.UseCtor]
		public MultiBrushToolLogic(
			Widget widget,
			World world,
			ModData modData,
			WorldRenderer worldRenderer,
			Dictionary<string, MiniYaml> logicArgs)
		{
			tool = world.WorldActor.Trait<MultiBrushTool>();
			if (!tool.IsEnabled)
				return;

			var editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			this.widget = (ScrollPanelWidget)widget;
			this.worldRenderer = worldRenderer;
			editor = widget.Parent.Parent.Parent.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			var editorLayer = world.WorldActor.Trait<EditorActorLayer>();

			var shapeChoices = Enum.GetValues<MultiBrushTool.ShapeType>();
			var brushTypeDropdown = widget
				.Get<ContainerWidget>("BRUSH_TYPE")
				.Get<DropDownButtonWidget>("DROPDOWN");
			brushTypeDropdown.GetText = tool.GetCurrentTypeLabel;
			brushTypeDropdown.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(MultiBrushTool.ShapeType choice, ScrollItemWidget template)
				{
					bool IsSelected() => choice == tool.CurrentShape;
					void OnClick() => tool.SetShape(choice);

					var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					var text = FluentProvider.GetMessage(tool.GetBrushTypeFluentKey(choice));
					item.Get<LabelWidget>("LABEL").GetText = () => text;
					return item;
				}

				brushTypeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", shapeChoices.Length * 30, shapeChoices, SetupItem);
			};

			Func<bool> isIsometricFunc = worldRenderer.World.Map.Grid.Type == MapGridType.RectangularIsometric ? () => true : () => false;

			var adaptHeightCheckbox = widget.GetOrNull<CheckboxWidget>("ADAPT_HEIGHT_CHECKBOX");
			if (adaptHeightCheckbox != null)
			{
				adaptHeightCheckbox.IsVisible = () => isIsometricFunc()
					&& tool.CurrentReplaceability.HasFlag(MultiBrush.Replaceability.Tile)
					&& tool.CurrentShape != MultiBrushTool.ShapeType.Flood;
				adaptHeightCheckbox.IsChecked = () => tool.CurrentAdaptHeight;
				adaptHeightCheckbox.OnClick = () => tool.ToggleAdaptHeight();
			}

			var sizeContainer = widget.Get<ContainerWidget>("BRUSH_SIZE");
			sizeContainer.IsVisible = () => tool.CurrentShape == MultiBrushTool.ShapeType.Circle || tool.CurrentShape == MultiBrushTool.ShapeType.Square;
			SetupSlider(sizeContainer, () => tool.CurrentSize.Width, (v) => tool.SetSize(new Size((int)v, (int)v)));

			var floodLimitContainer = widget.Get<ContainerWidget>("FLOOD_LIMIT");
			floodLimitContainer.IsVisible = () => tool.CurrentShape == MultiBrushTool.ShapeType.Flood;
			SetupSlider(floodLimitContainer, () => tool.CurrentFloodLimit, (v) => tool.SetFloodLimit((int)v));

			var brushDropdown = widget
				.Get<ContainerWidget>("BRUSH_PAINT_TYPE")
				.Get<DropDownButtonWidget>("DROPDOWN");
			brushDropdown.GetText = () => tool.CurrentMultiBrushCollection.Name;

			var brushChoices = tool.MultiBrushCollections.Keys.Order().ToImmutableArray();
			brushDropdown.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(string choice, ScrollItemWidget template)
				{
					bool IsSelected() => choice == tool.CurrentMultiBrushCollection.Name;
					void OnClick() => tool.SelectMultiBrush(choice);

					var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => choice;
					return item;
				}

				brushDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", brushChoices.Length * 30, brushChoices, SetupItem);
			};

			var floodBehaviorContainer = widget.Get<ContainerWidget>("FLOOD_TERRAIN_BEHAVIOR");
			floodBehaviorContainer.IsVisible = () => tool.CurrentShape == MultiBrushTool.ShapeType.Flood;

			var floodBehaviorDropdown = floodBehaviorContainer.Get<DropDownButtonWidget>("DROPDOWN");
			var floodBehaviorChoices = Enum.GetValues<MultiBrushTool.FloodTerrainBehavior>();
			floodBehaviorDropdown.GetText = tool.GetCurrentFloodBehaviorLabel;
			floodBehaviorDropdown.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(MultiBrushTool.FloodTerrainBehavior choice, ScrollItemWidget template)
				{
					bool IsSelected() => choice == tool.CurrentFloodBehavior;
					void OnClick() => tool.SetFloodBehavior(choice);
					var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					var text = FluentProvider.GetMessage(tool.GetFloodBehaviorFluentKey(choice));
					item.Get<LabelWidget>("LABEL").GetText = () => text;
					return item;
				}

				floodBehaviorDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", floodBehaviorChoices.Length * 30, floodBehaviorChoices, SetupItem);
			};

			var floodIgnoreHeightCheckbox = widget.GetOrNull<CheckboxWidget>("FLOOD_IGNORE_HEIGHT_CHECKBOX");
			if (floodIgnoreHeightCheckbox != null)
			{
				floodIgnoreHeightCheckbox.IsVisible = () => tool.CurrentShape == MultiBrushTool.ShapeType.Flood && isIsometricFunc();
				floodIgnoreHeightCheckbox.IsChecked = () => tool.CurrentFloodIgnoreHeight;
				floodIgnoreHeightCheckbox.OnClick = () => tool.ToggleFloodIgnoreHeight();
			}

			var floodIgnoreActorsCheckbox = widget.Get<CheckboxWidget>("FLOOD_IGNORE_ACTORS_CHECKBOX");
			floodIgnoreActorsCheckbox.IsVisible = () => tool.CurrentShape == MultiBrushTool.ShapeType.Flood;
			floodIgnoreActorsCheckbox.IsChecked = () => tool.CurrentFloodIgnoreActors;
			floodIgnoreActorsCheckbox.OnClick = () => tool.ToggleFloodIgnoreActors();

			var ownerContainer = widget.Get<ContainerWidget>("ACTOR_OWNER");
			ownerContainer.IsVisible = () =>
				tool.CurrentReplaceability.HasFlag(MultiBrush.Replaceability.Actor)
				|| tool.CurrentReplaceability.HasFlag(MultiBrush.Replaceability.SubCellActor);

			var ownersDropDown = ownerContainer.Get<DropDownButtonWidget>("DROPDOWN");
			ownersDropDown.OnClick = () =>
			{
				ScrollItemWidget SetupItem(PlayerReference option, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => tool.CurrentActorOwner == option, () => tool.SetActorOwner(option));
					item.Get<LabelWidget>("LABEL").GetText = () => option.Name;
					item.GetColor = () => option.Color;

					return item;
				}

				var owners = editorLayer.Players.Players.Values.OrderBy(p => p.Name);
				ownersDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, owners, SetupItem);
			};

			ownersDropDown.GetText = () => tool.CurrentActorOwner?.Name ?? "";
			ownersDropDown.TextColor = tool.CurrentActorOwner?.Color ?? Color.White;

			// Show the checkbox only if the current replaceability includes terrain and not only terrain.
			var filterTerrainCheckbox = widget.Get<CheckboxWidget>("FILTER_TERRAIN_CHECKBOX");
			filterTerrainCheckbox.IsChecked = () => !tool.CurrentReplaceability.HasFlag(MultiBrush.Replaceability.Tile);
			filterTerrainCheckbox.IsVisible = () =>
				tool.CurrentAvailableReplaceability.HasFlag(MultiBrush.Replaceability.Tile)
				&& tool.CurrentAvailableReplaceability != MultiBrush.Replaceability.Tile;
			filterTerrainCheckbox.OnClick = () => tool.ToggleReplacability(MultiBrush.Replaceability.Tile);

			var filterActorsCheckbox = widget.Get<CheckboxWidget>("FILTER_ACTORS_CHECKBOX");
			filterActorsCheckbox.IsChecked = () => !tool.CurrentReplaceability.HasFlag(MultiBrush.Replaceability.Actor);
			filterActorsCheckbox.IsVisible = () =>
				tool.CurrentAvailableReplaceability.HasFlag(MultiBrush.Replaceability.Actor)
				&& tool.CurrentAvailableReplaceability != MultiBrush.Replaceability.Actor;
			filterActorsCheckbox.OnClick = () => tool.ToggleReplacability(MultiBrush.Replaceability.Actor);

			var filterSubCellActorsCheckbox = widget.Get<CheckboxWidget>("FILTER_SUBCELL_ACTORS_CHECKBOX");
			filterSubCellActorsCheckbox.IsChecked = () => !tool.CurrentReplaceability.HasFlag(MultiBrush.Replaceability.SubCellActor);
			filterSubCellActorsCheckbox.IsVisible = () =>
				tool.CurrentAvailableReplaceability.HasFlag(MultiBrush.Replaceability.SubCellActor)
				&& tool.CurrentAvailableReplaceability != MultiBrush.Replaceability.SubCellActor;
			filterSubCellActorsCheckbox.OnClick = () => tool.ToggleReplacability(MultiBrush.Replaceability.SubCellActor);

			var sparsityContainer = widget.Get<ContainerWidget>("SPARSITY");
			sparsityContainer.IsVisible = () => tool.CurrentShape != MultiBrushTool.ShapeType.Single;
			SetupSlider(sparsityContainer, () => tool.CurrentSparsity / 10, (v) => tool.SetSparsity((int)v * 10));

			MapToolsLogic.OnSelected += TabSelected;
			tool.BlitRefreshed += RefreshLayout;

			RefreshLayout();
		}

		static void SetupSlider(ContainerWidget container, Func<float> getValue, Action<float> setValue)
		{
			var slider = container.Get<SliderWidget>("OPTION");
			var value = container.Get<TextFieldWidget>("VALUE");

			slider.GetValue = getValue;
			slider.OnChange += (v) =>
			{
				if (v != getValue())
					setValue(v);
			};

			void UpdateSparsityValueField(float f) => value.Text = ((int)f).ToString(NumberFormatInfo.CurrentInfo);
			UpdateSparsityValueField(getValue());
			slider.OnChange += UpdateSparsityValueField;

			value.OnTextEdited = () =>
			{
				if (float.TryParse(value.Text, out var result))
					slider.UpdateValue(result);
			};

			value.OnEscKey = _ => { value.YieldKeyboardFocus(); return true; };
			value.OnEnterKey = _ => { value.YieldKeyboardFocus(); return true; };
		}

		void TabSelected(bool isVisible)
		{
			if (isVisible && widget.IsVisible())
			{
				if (editor.CurrentBrush is not EditorPaintBrush)
					editor.SetBrush(new EditorPaintBrush(tool, editor, worldRenderer));
			}
			else if (editor.CurrentBrush is EditorPaintBrush)
			{
				editor.ClearBrush();
			}
		}

		void RefreshLayout()
		{
			widget.Layout.AdjustChildren();
		}

		protected override void Dispose(bool disposing)
		{
			if (tool.IsEnabled)
			{
				MapToolsLogic.OnSelected -= TabSelected;
				tool.BlitRefreshed -= RefreshLayout;
			}

			base.Dispose(disposing);
		}
	}
}
