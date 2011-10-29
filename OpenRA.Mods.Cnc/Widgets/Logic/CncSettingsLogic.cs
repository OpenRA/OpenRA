#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncSettingsLogic
	{
		enum PanelType { General, Input }

		PanelType Settings = PanelType.General;
		ColorPickerPaletteModifier playerPalettePreview;
		World world;

		[ObjectCreator.UseCtor]
		public CncSettingsLogic(Widget widget, World world, Action onExit)
		{
			this.world = world;
			var panel = widget.GetWidget("SETTINGS_PANEL");

			// General pane
			var generalButton = panel.GetWidget<ButtonWidget>("GENERAL_BUTTON");
			generalButton.OnClick = () => Settings = PanelType.General;
			generalButton.IsDisabled = () => Settings == PanelType.General;

			var generalPane = panel.GetWidget("GENERAL_CONTROLS");
			generalPane.IsVisible = () => Settings == PanelType.General;

			var gameSettings = Game.Settings.Game;
			var playerSettings = Game.Settings.Player;
			var debugSettings = Game.Settings.Debug;
			var graphicsSettings = Game.Settings.Graphics;
			var soundSettings = Game.Settings.Sound;

			// Player profile
			var nameTextfield = generalPane.GetWidget<TextFieldWidget>("NAME_TEXTFIELD");
			nameTextfield.Text = playerSettings.Name;

			playerPalettePreview = world.WorldActor.Trait<ColorPickerPaletteModifier>();
			playerPalettePreview.Ramp = playerSettings.ColorRamp;

			var colorDropdown = generalPane.GetWidget<DropDownButtonWidget>("COLOR_DROPDOWN");
			colorDropdown.OnMouseDown = _ => ShowColorPicker(colorDropdown, playerSettings);
			colorDropdown.GetWidget<ColorBlockWidget>("COLORBLOCK").GetColor = () => playerSettings.ColorRamp.GetColor(0);

			// Debug
			var perftextCheckbox = generalPane.GetWidget<CheckboxWidget>("PERFTEXT_CHECKBOX");
			perftextCheckbox.IsChecked = () => debugSettings.PerfText;
			perftextCheckbox.OnClick = () => debugSettings.PerfText ^= true;

			var perfgraphCheckbox = generalPane.GetWidget<CheckboxWidget>("PERFGRAPH_CHECKBOX");
			perfgraphCheckbox.IsChecked = () => debugSettings.PerfGraph;
			perfgraphCheckbox.OnClick = () => debugSettings.PerfGraph ^= true;

			var checkunsyncedCheckbox = generalPane.GetWidget<CheckboxWidget>("CHECKUNSYNCED_CHECKBOX");
			checkunsyncedCheckbox.IsChecked = () => debugSettings.SanityCheckUnsyncedCode;
			checkunsyncedCheckbox.OnClick = () => debugSettings.SanityCheckUnsyncedCode ^= true;

			// Video
			var windowModeDropdown = generalPane.GetWidget<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => SettingsMenuLogic.ShowWindowModeDropdown(windowModeDropdown, graphicsSettings);
			windowModeDropdown.GetText = () => graphicsSettings.Mode == WindowMode.Windowed ?
				"Windowed" : graphicsSettings.Mode == WindowMode.Fullscreen ? "Fullscreen" : "Pseudo-Fullscreen";

			var pixelDoubleCheckbox = generalPane.GetWidget<CheckboxWidget>("PIXELDOUBLE_CHECKBOX");
			pixelDoubleCheckbox.IsChecked = () => graphicsSettings.PixelDouble;
			pixelDoubleCheckbox.OnClick = () =>
			{
				graphicsSettings.PixelDouble ^= true;
				Game.viewport.Zoom = graphicsSettings.PixelDouble ? 2 : 1;
			};

			generalPane.GetWidget("WINDOW_RESOLUTION").IsVisible = () => graphicsSettings.Mode == WindowMode.Windowed;
			var windowWidth = generalPane.GetWidget<TextFieldWidget>("WINDOW_WIDTH");
			windowWidth.Text = graphicsSettings.WindowedSize.X.ToString();

			var windowHeight = generalPane.GetWidget<TextFieldWidget>("WINDOW_HEIGHT");
			windowHeight.Text = graphicsSettings.WindowedSize.Y.ToString();

			// Audio
			var soundSlider = generalPane.GetWidget<SliderWidget>("SOUND_SLIDER");
			soundSlider.OnChange += x => { soundSettings.SoundVolume = x; Sound.SoundVolume = x;};
			soundSlider.Value = soundSettings.SoundVolume;

			var musicSlider = generalPane.GetWidget<SliderWidget>("MUSIC_SLIDER");
			musicSlider.OnChange += x => { soundSettings.MusicVolume = x; Sound.MusicVolume = x; };
			musicSlider.Value = soundSettings.MusicVolume;

			var shellmapMusicCheckbox = generalPane.GetWidget<CheckboxWidget>("SHELLMAP_MUSIC");
			shellmapMusicCheckbox.IsChecked = () => soundSettings.ShellmapMusic;
			shellmapMusicCheckbox.OnClick = () => soundSettings.ShellmapMusic ^= true;

			// Input pane
			var inputPane = panel.GetWidget("INPUT_CONTROLS");
			inputPane.IsVisible = () => Settings == PanelType.Input;

			var inputButton = panel.GetWidget<ButtonWidget>("INPUT_BUTTON");
			inputButton.OnClick = () => Settings = PanelType.Input;
			inputButton.IsDisabled = () => Settings == PanelType.Input;

			inputPane.GetWidget<CheckboxWidget>("CLASSICORDERS_CHECKBOX").IsDisabled = () => true;

			var scrollSlider = inputPane.GetWidget<SliderWidget>("SCROLLSPEED_SLIDER");
			scrollSlider.Value = gameSettings.ViewportEdgeScrollStep;
			scrollSlider.OnChange += x => gameSettings.ViewportEdgeScrollStep = x;

			var edgescrollCheckbox = inputPane.GetWidget<CheckboxWidget>("EDGESCROLL_CHECKBOX");
			edgescrollCheckbox.IsChecked = () => gameSettings.ViewportEdgeScroll;
			edgescrollCheckbox.OnClick = () => gameSettings.ViewportEdgeScroll ^= true;

			var mouseScrollDropdown = inputPane.GetWidget<DropDownButtonWidget>("MOUSE_SCROLL");
			mouseScrollDropdown.OnMouseDown = _ => ShowMouseScrollDropdown(mouseScrollDropdown, gameSettings);
			mouseScrollDropdown.GetText = () => gameSettings.MouseScroll.ToString();

			var teamchatCheckbox = inputPane.GetWidget<CheckboxWidget>("TEAMCHAT_CHECKBOX");
			teamchatCheckbox.IsChecked = () => gameSettings.TeamChatToggle;
			teamchatCheckbox.OnClick = () => gameSettings.TeamChatToggle ^= true;

			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				playerSettings.Name = nameTextfield.Text;
				int x, y;
				int.TryParse(windowWidth.Text, out x);
				int.TryParse(windowHeight.Text, out y);
				graphicsSettings.WindowedSize = new int2(x,y);
				Game.Settings.Save();
				Widget.CloseWindow();
				onExit();
			};
		}

		bool ShowColorPicker(DropDownButtonWidget color, PlayerSettings s)
		{
			Action<ColorRamp> onSelect = c => { s.ColorRamp = c; color.RemovePanel(); };
			Action<ColorRamp> onChange = c => {	playerPalettePreview.Ramp = c; };

			var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onSelect", onSelect },
				{ "onChange", onChange },
				{ "initialRamp", s.ColorRamp }
			});

			color.AttachPanel(colorChooser);
			return true;
		}

		static bool ShowMouseScrollDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, MouseScrollType>()
			{
				{ "Disabled", MouseScrollType.Disabled },
				{ "Standard", MouseScrollType.Standard },
				{ "Inverted", MouseScrollType.Inverted },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.MouseScroll == options[o],
					() => s.MouseScroll = options[o]);
				item.GetWidget<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
			return true;
		}
	}
}
