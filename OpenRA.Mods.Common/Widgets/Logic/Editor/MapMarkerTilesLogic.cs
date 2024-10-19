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
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;
using static OpenRA.Mods.Common.Traits.MarkerLayerOverlay;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapMarkerTilesLogic : ChromeLogic
	{
		[FluentReference]
		const string MarkerMirrorModeNone = "mirror-mode.none";

		[FluentReference]
		const string MarkerMirrorModeFlip = "mirror-mode.flip";

		[FluentReference]
		const string MarkerMirrorModeRotate = "mirror-mode.rotate";

		readonly EditorActionManager editorActionManager;
		readonly MarkerLayerOverlay markerLayerTrait;
		readonly ScrollPanelWidget tileColorPanel;
		readonly SliderWidget alphaSlider;
		readonly LabelWidget alphaValueLabel;
		readonly DropDownButtonWidget modeDropdown;
		readonly SliderWidget rotateNumSidesSlider;
		readonly DropDownButtonWidget flipNumSidesDropdown;
		readonly LabelWidget numSidesLabel;
		readonly LabelWidget rotateNumSidesValueLabel;
		readonly LabelWidget axisAngleLabel;
		readonly SliderWidget axisAngleSlider;
		readonly LabelWidget axisAngleValueLabel;
		readonly ButtonWidget clearSelectedButtonWidget;
		readonly ButtonWidget clearAllButtonWidget;
		readonly EditorViewportControllerWidget editor;

		int? markerTile;

		[ObjectCreator.UseCtor]
		public MapMarkerTilesLogic(Widget widget, World world, ModData modData, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
		{
			markerLayerTrait = world.WorldActor.Trait<MarkerLayerOverlay>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			editor = widget.Parent.Parent.Parent.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editor.BrushChanged += HandleBrushChanged;

			tileColorPanel = widget.Get<ScrollPanelWidget>("TILE_COLOR_PANEL");
			{
				tileColorPanel.Layout = new GridLayout(tileColorPanel);
				var colorSwatchTemplate = tileColorPanel.Get<ScrollItemWidget>("TILE_COLOR_TEMPLATE");
				var iconTemplate = tileColorPanel.Get<ScrollItemWidget>("TILE_ICON_TEMPLATE");
				tileColorPanel.RemoveChildren();

				var colors = markerLayerTrait.Info.Colors;
				for (var colorIndex = 0; colorIndex < colors.Length; colorIndex++)
				{
					var scrollItem = SetupColorSwatchItem(colorIndex, colorSwatchTemplate);
					tileColorPanel.AddChild(scrollItem);
				}

				var eraseItem = SetupEraseItem(iconTemplate);
				tileColorPanel.AddChild(eraseItem);

				///////

				ScrollItemWidget SetupColorSwatchItem(int index, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template,
						() => markerTile == index,
						() =>
						{
							markerTile = index;
							editor.SetBrush(new EditorMarkerLayerBrush(editor, index, worldRenderer));
						});

					var colorWidget = item.Get<ColorBlockWidget>("TILE_PREVIEW");
					colorWidget.GetColor = () => colors[index];

					return item;
				}

				ScrollItemWidget SetupEraseItem(ScrollItemWidget template)
				{
					return ScrollItemWidget.Setup(template,
						() => markerTile == null && editor.CurrentBrush is EditorMarkerLayerBrush,
						() =>
						{
							markerTile = null;
							editor.SetBrush(new EditorMarkerLayerBrush(editor, null, worldRenderer));
						});
				}
			}

			clearSelectedButtonWidget = widget.Get<ButtonWidget>("CLEAR_CURRENT_BUTTON");
			clearSelectedButtonWidget.IsDisabled = () => markerTile == null;
			clearSelectedButtonWidget.OnClick = ClearSelected;

			clearAllButtonWidget = widget.Get<ButtonWidget>("CLEAR_ALL_BUTTON");
			clearAllButtonWidget.OnClick = ClearAll;

			alphaSlider = widget.Get<SliderWidget>("ALPHA_SLIDER");
			alphaSlider.MinimumValue = 1;
			alphaSlider.MaximumValue = 255;
			alphaSlider.Ticks = 12;
			alphaSlider.OnChange += (val) => markerLayerTrait.TileAlpha = (int)val;
			alphaSlider.GetValue = () => markerLayerTrait.TileAlpha;

			alphaValueLabel = widget.Get<LabelWidget>("ALPHA_VALUE");
			alphaValueLabel.GetText = () => markerLayerTrait.TileAlpha.ToString(NumberFormatInfo.InvariantInfo);

			modeDropdown = widget.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			modeDropdown.OnMouseDown = _ => ShowMarkerModeDropDown(modeDropdown);
			modeDropdown.GetText = () =>
			{
				switch (markerLayerTrait.MirrorMode)
				{
					case MarkerTileMirrorMode.None:
						return FluentProvider.GetMessage(MarkerMirrorModeNone);
					case MarkerTileMirrorMode.Flip:
						return FluentProvider.GetMessage(MarkerMirrorModeFlip);
					case MarkerTileMirrorMode.Rotate:
						return FluentProvider.GetMessage(MarkerMirrorModeRotate);
					default:
						throw new ArgumentException($"Couldn't find fluent string for marker tile mirror mode '{markerLayerTrait.MirrorMode}'");
				}
			};

			bool IsFlipMode() => markerLayerTrait.MirrorMode == MarkerTileMirrorMode.Flip;
			bool IsRotateMode() => markerLayerTrait.MirrorMode == MarkerTileMirrorMode.Rotate;

			numSidesLabel = widget.Get<LabelWidget>("NUM_SIDES_LABEL");
			numSidesLabel.IsVisible = () => IsFlipMode() || IsRotateMode();

			rotateNumSidesSlider = widget.Get<SliderWidget>("ROTATE_NUM_SIDES_SLIDER");
			rotateNumSidesSlider.MinimumValue = 2;
			rotateNumSidesSlider.MaximumValue = 8;
			rotateNumSidesSlider.Ticks = 7;
			rotateNumSidesSlider.IsVisible = IsRotateMode;
			rotateNumSidesSlider.OnChange += (val) => markerLayerTrait.NumSides = (int)val;
			rotateNumSidesSlider.GetValue = () => markerLayerTrait.NumSides;

			rotateNumSidesValueLabel = widget.Get<LabelWidget>("ROTATE_NUM_SIDES_VALUE");
			rotateNumSidesValueLabel.IsVisible = IsRotateMode;
			rotateNumSidesValueLabel.GetText = () => markerLayerTrait.NumSides.ToString(NumberFormatInfo.InvariantInfo);

			flipNumSidesDropdown = widget.Get<DropDownButtonWidget>("FLIP_NUM_SIDES_DROPDOWN");
			flipNumSidesDropdown.OnMouseDown = _ => ShowFlipNumSidesDropDown(flipNumSidesDropdown);
			flipNumSidesDropdown.IsVisible = IsFlipMode;
			flipNumSidesDropdown.GetText = () => markerLayerTrait.NumSides.ToString(NumberFormatInfo.InvariantInfo);

			axisAngleLabel = widget.Get<LabelWidget>("AXIS_ANGLE_LABEL");
			axisAngleLabel.IsVisible = IsFlipMode;

			axisAngleSlider = widget.Get<SliderWidget>("AXIS_ANGLE_SLIDER");
			axisAngleSlider.MinimumValue = 0;
			axisAngleSlider.MaximumValue = 11;
			axisAngleSlider.Ticks = 12;
			axisAngleSlider.IsVisible = IsFlipMode;
			axisAngleSlider.OnChange += (val) => markerLayerTrait.AxisAngle = (int)val * 15;
			axisAngleSlider.GetValue = () => markerLayerTrait.AxisAngle / 15;

			axisAngleValueLabel = widget.Get<LabelWidget>("AXIS_ANGLE_VALUE");
			axisAngleValueLabel.IsVisible = IsFlipMode;
			axisAngleValueLabel.GetText = () => markerLayerTrait.AxisAngle.ToString(NumberFormatInfo.InvariantInfo);
		}

		protected override void Dispose(bool disposing)
		{
			editor.BrushChanged -= HandleBrushChanged;
			base.Dispose(disposing);
		}

		void HandleBrushChanged()
		{
			if (editor.CurrentBrush is not EditorMarkerLayerBrush)
			{
				markerTile = null;
			}
		}

		void ClearSelected()
		{
			if (editor.CurrentBrush is EditorMarkerLayerBrush markerLayerBrush &&
				markerLayerBrush.Template.HasValue &&
				markerLayerTrait.Tiles.TryGetValue(markerLayerBrush.Template.Value, out var tiles) &&
				tiles.Count > 0)
				editorActionManager.Add(new ClearSelectedMarkerTilesEditorAction(markerLayerBrush.Template.Value, markerLayerTrait));
		}

		void ClearAll()
		{
			if (markerLayerTrait.Tiles.Count > 0 && markerLayerTrait.Tiles.Any(x => x.Value.Count > 0))
				editorActionManager.Add(new ClearAllMarkerTilesEditorAction(markerLayerTrait));
		}

		void ShowMarkerModeDropDown(DropDownButtonWidget dropdown)
		{
			ScrollItemWidget SetupItem(MarkerTileMirrorMode mode, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => markerLayerTrait.MirrorMode == mode,
					() => markerLayerTrait.SetMirrorMode(mode));

				item.Get<LabelWidget>("LABEL").GetText = () =>
				{
					switch (mode)
					{
						case MarkerTileMirrorMode.None:
							return FluentProvider.GetMessage(MarkerMirrorModeNone);
						case MarkerTileMirrorMode.Flip:
							return FluentProvider.GetMessage(MarkerMirrorModeFlip);
						case MarkerTileMirrorMode.Rotate:
							return FluentProvider.GetMessage(MarkerMirrorModeRotate);
						default:
							throw new ArgumentException($"Couldn't find fluent string for marker tile mirror mode '{mode}'");
					}
				};

				return item;
			}

			var options = new[] { MarkerTileMirrorMode.None, MarkerTileMirrorMode.Flip, MarkerTileMirrorMode.Rotate };
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, options, SetupItem);
		}

		void ShowFlipNumSidesDropDown(DropDownButtonWidget dropdown)
		{
			ScrollItemWidget SetupItem(int value, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => markerLayerTrait.NumSides == value,
					() => markerLayerTrait.NumSides = value);

				item.Get<LabelWidget>("LABEL").GetText = () => value.ToString(NumberFormatInfo.InvariantInfo);
				return item;
			}

			var options = new[] { 2, 4 };
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, options, SetupItem);
		}
	}
}
