#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class InputSettingsLogic : ChromeLogic
	{
		static InputSettingsLogic() { }

		[ObjectCreator.UseCtor]
		public InputSettingsLogic(Action<string, string, Func<Widget, Func<bool>>, Func<Widget, Action>> registerPanel, string panelID, string label)
		{
			registerPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var gs = Game.Settings.Game;

			SettingsUtils.BindCheckboxPref(panel, "ALTERNATE_SCROLL_CHECKBOX", gs, "UseAlternateScrollButton");
			SettingsUtils.BindCheckboxPref(panel, "EDGESCROLL_CHECKBOX", gs, "ViewportEdgeScroll");
			SettingsUtils.BindCheckboxPref(panel, "LOCKMOUSE_CHECKBOX", gs, "LockMouseWindow");
			SettingsUtils.BindSliderPref(panel, "ZOOMSPEED_SLIDER", gs, "ZoomSpeed");
			SettingsUtils.BindSliderPref(panel, "SCROLLSPEED_SLIDER", gs, "ViewportEdgeScrollStep");
			SettingsUtils.BindSliderPref(panel, "UI_SCROLLSPEED_SLIDER", gs, "UIScrollSpeed");

			var mouseControlDropdown = panel.Get<DropDownButtonWidget>("MOUSE_CONTROL_DROPDOWN");
			mouseControlDropdown.OnMouseDown = _ => ShowMouseControlDropdown(mouseControlDropdown, gs);
			mouseControlDropdown.GetText = () => gs.UseClassicMouseStyle ? "Classic" : "Modern";

			var mouseScrollDropdown = panel.Get<DropDownButtonWidget>("MOUSE_SCROLL_TYPE_DROPDOWN");
			mouseScrollDropdown.OnMouseDown = _ => ShowMouseScrollDropdown(mouseScrollDropdown, gs);
			mouseScrollDropdown.GetText = () => gs.MouseScroll.ToString();

			var mouseControlDescClassic = panel.Get("MOUSE_CONTROL_DESC_CLASSIC");
			mouseControlDescClassic.IsVisible = () => gs.UseClassicMouseStyle;

			var mouseControlDescModern = panel.Get("MOUSE_CONTROL_DESC_MODERN");
			mouseControlDescModern.IsVisible = () => !gs.UseClassicMouseStyle;

			foreach (var container in new[] { mouseControlDescClassic, mouseControlDescModern })
			{
				var classicScrollRight = container.Get("DESC_SCROLL_RIGHT");
				classicScrollRight.IsVisible = () => gs.UseClassicMouseStyle ^ gs.UseAlternateScrollButton;

				var classicScrollMiddle = container.Get("DESC_SCROLL_MIDDLE");
				classicScrollMiddle.IsVisible = () => !gs.UseClassicMouseStyle ^ gs.UseAlternateScrollButton;

				var zoomDesc = container.Get("DESC_ZOOM");
				zoomDesc.IsVisible = () => gs.ZoomModifier == Modifiers.None;

				var zoomDescModifier = container.Get<LabelWidget>("DESC_ZOOM_MODIFIER");
				zoomDescModifier.IsVisible = () => gs.ZoomModifier != Modifiers.None;

				var zoomDescModifierTemplate = zoomDescModifier.Text;
				var zoomDescModifierLabel = new CachedTransform<Modifiers, string>(
					mod => zoomDescModifierTemplate.Replace("MODIFIER", mod.ToString()));
				zoomDescModifier.GetText = () => zoomDescModifierLabel.Update(gs.ZoomModifier);

				var edgescrollDesc = container.Get<LabelWidget>("DESC_EDGESCROLL");
				edgescrollDesc.IsVisible = () => gs.ViewportEdgeScroll;
			}

			// Apply mouse focus preferences immediately
			var lockMouseCheckbox = panel.Get<CheckboxWidget>("LOCKMOUSE_CHECKBOX");
			var oldOnClick = lockMouseCheckbox.OnClick;
			lockMouseCheckbox.OnClick = () =>
			{
				// Still perform the old behaviour for clicking the checkbox, before
				// applying the changes live.
				oldOnClick();

				MakeMouseFocusSettingsLive();
			};

			var zoomModifierDropdown = panel.Get<DropDownButtonWidget>("ZOOM_MODIFIER");
			zoomModifierDropdown.OnMouseDown = _ => ShowZoomModifierDropdown(zoomModifierDropdown, gs);
			zoomModifierDropdown.GetText = () => gs.ZoomModifier.ToString();

			return () => false;
		}

		Action ResetPanel(Widget panel)
		{
			var gs = Game.Settings.Game;
			var dgs = new GameSettings();

			return () =>
			{
				gs.UseClassicMouseStyle = dgs.UseClassicMouseStyle;
				gs.MouseScroll = dgs.MouseScroll;
				gs.UseAlternateScrollButton = dgs.UseAlternateScrollButton;
				gs.LockMouseWindow = dgs.LockMouseWindow;
				gs.ViewportEdgeScroll = dgs.ViewportEdgeScroll;
				gs.ViewportEdgeScrollStep = dgs.ViewportEdgeScrollStep;
				gs.ZoomSpeed = dgs.ZoomSpeed;
				gs.UIScrollSpeed = dgs.UIScrollSpeed;
				gs.ZoomModifier = dgs.ZoomModifier;

				panel.Get<SliderWidget>("SCROLLSPEED_SLIDER").Value = gs.ViewportEdgeScrollStep;
				panel.Get<SliderWidget>("UI_SCROLLSPEED_SLIDER").Value = gs.UIScrollSpeed;

				MakeMouseFocusSettingsLive();
			};
		}

		public static void ShowMouseControlDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, bool>()
			{
				{ "Classic", true },
				{ "Modern", false },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.UseClassicMouseStyle == options[o],
					() => s.UseClassicMouseStyle = options[o]);
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
		}

		static void ShowMouseScrollDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, MouseScrollType>()
			{
				{ "Disabled", MouseScrollType.Disabled },
				{ "Standard", MouseScrollType.Standard },
				{ "Inverted", MouseScrollType.Inverted },
				{ "Joystick", MouseScrollType.Joystick },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.MouseScroll == options[o],
					() => s.MouseScroll = options[o]);
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
		}

		static void ShowZoomModifierDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, Modifiers>()
			{
				{ "Alt", Modifiers.Alt },
				{ "Ctrl", Modifiers.Ctrl },
				{ "Meta", Modifiers.Meta },
				{ "Shift", Modifiers.Shift },
				{ "None", Modifiers.None }
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.ZoomModifier == options[o],
					() => s.ZoomModifier = options[o]);
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
		}

		void MakeMouseFocusSettingsLive()
		{
			var gameSettings = Game.Settings.Game;

			if (gameSettings.LockMouseWindow)
				Game.Renderer.GrabWindowMouseFocus();
			else
				Game.Renderer.ReleaseWindowMouseFocus();
		}
	}
}
