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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class InputSettingsLogic : ChromeLogic
	{
		[FluentReference]
		const string Classic = "options-control-scheme.classic";

		[FluentReference]
		const string Modern = "options-control-scheme.modern";

		[FluentReference]
		const string OtherRTS = "options-control-scheme.otherrts";

		[FluentReference]
		const string Disabled = "options-mouse-scroll-type.disabled";

		[FluentReference]
		const string Standard = "options-mouse-scroll-type.standard";

		[FluentReference]
		const string Inverted = "options-mouse-scroll-type.inverted";

		[FluentReference]
		const string Joystick = "options-mouse-scroll-type.joystick";

		readonly GameSettings gameSettings;

		readonly Dictionary<MouseControlStyle, string> controlTypes;

		[ObjectCreator.UseCtor]
		public InputSettingsLogic(ModData modData, SettingsLogic settingsLogic, string panelID, string label)
		{
			controlTypes = new Dictionary<MouseControlStyle, string>
			{
				{ MouseControlStyle.Classic, FluentProvider.GetMessage(Classic) },
				{ MouseControlStyle.Modern, FluentProvider.GetMessage(Modern) },
				{ MouseControlStyle.OtherRTS, FluentProvider.GetMessage(OtherRTS) },
			};
			gameSettings = modData.GetSettings<GameSettings>();

			settingsLogic.RegisterSettingsPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");

			SettingsUtils.BindCheckboxPref(panel, "ALTERNATE_SCROLL_CHECKBOX", gameSettings, "UseAlternateScrollButton");
			SettingsUtils.BindCheckboxPref(panel, "EDGESCROLL_CHECKBOX", gameSettings, "ViewportEdgeScroll");
			SettingsUtils.BindCheckboxPref(panel, "LOCKMOUSE_CHECKBOX", gameSettings, "LockMouseWindow");
			SettingsUtils.BindSliderPref(panel, "ZOOMSPEED_SLIDER", gameSettings, "ZoomSpeed");
			SettingsUtils.BindSliderPref(panel, "SCROLLSPEED_SLIDER", gameSettings, "ViewportEdgeScrollStep");
			SettingsUtils.BindSliderPref(panel, "UI_SCROLLSPEED_SLIDER", gameSettings, "UIScrollSpeed");

			var mouseControlDropdown = panel.Get<DropDownButtonWidget>("MOUSE_CONTROL_DROPDOWN");
			mouseControlDropdown.OnMouseDown = _ => ShowMouseControlDropdown(mouseControlDropdown, controlTypes, gameSettings);
			mouseControlDropdown.GetText = () => controlTypes[gameSettings.MouseControlStyle];

			var mouseScrollDropdown = panel.Get<DropDownButtonWidget>("MOUSE_SCROLL_TYPE_DROPDOWN");
			mouseScrollDropdown.OnMouseDown = _ => ShowMouseScrollDropdown(mouseScrollDropdown, gameSettings);

			// MouseScroll can change, must display latest value.
#pragma warning disable IDE0200 // Remove unnecessary lambda expression
			mouseScrollDropdown.GetText = () => gameSettings.MouseScroll.ToString();
#pragma warning restore IDE0200

			var mouseControlDescClassic = panel.Get("MOUSE_CONTROL_DESC_CLASSIC");
			mouseControlDescClassic.IsVisible = () => gameSettings.MouseControlStyle == MouseControlStyle.Classic;

			var mouseControlDescModern = panel.Get("MOUSE_CONTROL_DESC_MODERN");
			mouseControlDescModern.IsVisible = () => gameSettings.MouseControlStyle == MouseControlStyle.Modern;

			var mouseControlDescOtherRTS = panel.Get("MOUSE_CONTROL_DESC_OTHERRTS");
			mouseControlDescOtherRTS.IsVisible = () => gameSettings.MouseControlStyle == MouseControlStyle.OtherRTS;

			foreach (var container in new[] { mouseControlDescClassic, mouseControlDescModern, mouseControlDescOtherRTS })
			{
				var classicScrollRight = container.Get("DESC_SCROLL_RIGHT");
				classicScrollRight.IsVisible = () => (gameSettings.MouseControlStyle == MouseControlStyle.Classic) ^ gameSettings.UseAlternateScrollButton;

				var classicScrollMiddle = container.Get("DESC_SCROLL_MIDDLE");
				classicScrollMiddle.IsVisible = () => (gameSettings.MouseControlStyle != MouseControlStyle.Classic) ^ gameSettings.UseAlternateScrollButton;

				var zoomDesc = container.Get("DESC_ZOOM");
				zoomDesc.IsVisible = () => gameSettings.ZoomModifier == Modifiers.None;

				var zoomDescModifier = container.Get<LabelWidget>("DESC_ZOOM_MODIFIER");
				zoomDescModifier.IsVisible = () => gameSettings.ZoomModifier != Modifiers.None;

				var zoomDescModifierTemplate = zoomDescModifier.GetText();
				var zoomDescModifierLabel = new CachedTransform<Modifiers, string>(
					mod => zoomDescModifierTemplate.Replace("MODIFIER", mod.ToString()));
				zoomDescModifier.GetText = () => zoomDescModifierLabel.Update(gameSettings.ZoomModifier);

				var edgescrollDesc = container.Get<LabelWidget>("DESC_EDGESCROLL");
				edgescrollDesc.IsVisible = () => gameSettings.ViewportEdgeScroll;
			}

			// Apply mouse focus preferences immediately
			var lockMouseCheckbox = panel.Get<CheckboxWidget>("LOCKMOUSE_CHECKBOX");
			var oldOnClick = lockMouseCheckbox.OnClick;
			lockMouseCheckbox.OnClick = () =>
			{
				// Still perform the old behaviour for clicking the checkbox, before
				// applying the changes live.
				oldOnClick();

				MakeMouseFocusSettingsLive(gameSettings);
			};

			var zoomModifierDropdown = panel.Get<DropDownButtonWidget>("ZOOM_MODIFIER");
			zoomModifierDropdown.OnMouseDown = _ => ShowZoomModifierDropdown(zoomModifierDropdown, gameSettings);

			// ZoomModifier can change, must display latest value.
#pragma warning disable IDE0200 // Remove unnecessary lambda expression
			zoomModifierDropdown.GetText = () => gameSettings.ZoomModifier.ToString();
#pragma warning restore IDE0200

			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			return () => false;
		}

		Action ResetPanel(Widget panel)
		{
			var defaultGameSettings = new GameSettings();

			return () =>
			{
				gameSettings.MouseControlStyle = defaultGameSettings.MouseControlStyle;
				gameSettings.MouseScroll = defaultGameSettings.MouseScroll;
				gameSettings.UseAlternateScrollButton = defaultGameSettings.UseAlternateScrollButton;
				gameSettings.LockMouseWindow = defaultGameSettings.LockMouseWindow;
				gameSettings.ViewportEdgeScroll = defaultGameSettings.ViewportEdgeScroll;
				gameSettings.ViewportEdgeScrollStep = defaultGameSettings.ViewportEdgeScrollStep;
				gameSettings.ZoomSpeed = defaultGameSettings.ZoomSpeed;
				gameSettings.UIScrollSpeed = defaultGameSettings.UIScrollSpeed;
				gameSettings.ZoomModifier = defaultGameSettings.ZoomModifier;

				panel.Get<SliderWidget>("SCROLLSPEED_SLIDER").Value = gameSettings.ViewportEdgeScrollStep;
				panel.Get<SliderWidget>("UI_SCROLLSPEED_SLIDER").Value = gameSettings.UIScrollSpeed;

				MakeMouseFocusSettingsLive(gameSettings);
			};
		}

		public static void ShowMouseControlDropdown(DropDownButtonWidget dropdown, Dictionary<MouseControlStyle, string> controlTypes, GameSettings gameSettings)
		{
			ScrollItemWidget SetupItem(MouseControlStyle o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gameSettings.MouseControlStyle == o,
					() => gameSettings.MouseControlStyle = o);

				var label = controlTypes[o];
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, controlTypes.Keys, SetupItem);
		}

		static void ShowMouseScrollDropdown(DropDownButtonWidget dropdown, GameSettings gameSettings)
		{
			var options = new Dictionary<string, MouseScrollType>()
			{
				{ FluentProvider.GetMessage(Disabled), MouseScrollType.Disabled },
				{ FluentProvider.GetMessage(Standard), MouseScrollType.Standard },
				{ FluentProvider.GetMessage(Inverted), MouseScrollType.Inverted },
				{ FluentProvider.GetMessage(Joystick), MouseScrollType.Joystick },
			};

			ScrollItemWidget SetupItem(string o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gameSettings.MouseScroll == options[o],
					() => gameSettings.MouseScroll = options[o]);
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, SetupItem);
		}

		static void ShowZoomModifierDropdown(DropDownButtonWidget dropdown, GameSettings gameSettings)
		{
			var options = new Dictionary<string, Modifiers>()
			{
				{ ModifiersExts.DisplayString(Modifiers.Alt), Modifiers.Alt },
				{ ModifiersExts.DisplayString(Modifiers.Ctrl), Modifiers.Ctrl },
				{ ModifiersExts.DisplayString(Modifiers.Meta), Modifiers.Meta },
				{ ModifiersExts.DisplayString(Modifiers.Shift), Modifiers.Shift },
				{ ModifiersExts.DisplayString(Modifiers.None), Modifiers.None }
			};

			ScrollItemWidget SetupItem(string o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gameSettings.ZoomModifier == options[o],
					() => gameSettings.ZoomModifier = options[o]);
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, SetupItem);
		}

		static void MakeMouseFocusSettingsLive(GameSettings gameSettings)
		{
			if (gameSettings.LockMouseWindow)
				Game.Renderer.GrabWindowMouseFocus();
			else
				Game.Renderer.ReleaseWindowMouseFocus();
		}
	}
}
