#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.RA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncSettingsLogic
	{
		enum PanelType { General, Input }

		PanelType Settings = PanelType.General;
		ColorPreviewManagerWidget colorPreview;
		World world;

		[ObjectCreator.UseCtor]
		public CncSettingsLogic(Widget widget, World world, Action onExit)
		{
			this.world = world;
			var panel = widget.Get("SETTINGS_PANEL");

			// General pane
			var generalButton = panel.Get<ButtonWidget>("GENERAL_BUTTON");
			generalButton.OnClick = () => Settings = PanelType.General;
			generalButton.IsHighlighted = () => Settings == PanelType.General;

			var generalPane = panel.Get("GENERAL_CONTROLS");
			generalPane.IsVisible = () => Settings == PanelType.General;

			var gameSettings = Game.Settings.Game;
			var playerSettings = Game.Settings.Player;
			var debugSettings = Game.Settings.Debug;
			var graphicsSettings = Game.Settings.Graphics;
			var soundSettings = Game.Settings.Sound;

			// Player profile
			var nameTextfield = generalPane.Get<TextFieldWidget>("NAME_TEXTFIELD");
			nameTextfield.Text = playerSettings.Name;

			colorPreview = panel.Get<ColorPreviewManagerWidget>("COLOR_MANAGER");
			colorPreview.Ramp = playerSettings.ColorRamp;

			var colorDropdown = generalPane.Get<DropDownButtonWidget>("COLOR");
			colorDropdown.OnMouseDown = _ => ShowColorPicker(colorDropdown, playerSettings);
			colorDropdown.Get<ColorBlockWidget>("COLORBLOCK").GetColor = () => playerSettings.ColorRamp.GetColor(0);

			// Debug
			var perftextCheckbox = generalPane.Get<CheckboxWidget>("PERFTEXT_CHECKBOX");
			perftextCheckbox.IsChecked = () => debugSettings.PerfText;
			perftextCheckbox.OnClick = () => debugSettings.PerfText ^= true;

			var perfgraphCheckbox = generalPane.Get<CheckboxWidget>("PERFGRAPH_CHECKBOX");
			perfgraphCheckbox.IsChecked = () => debugSettings.PerfGraph;
			perfgraphCheckbox.OnClick = () => debugSettings.PerfGraph ^= true;

			var checkunsyncedCheckbox = generalPane.Get<CheckboxWidget>("CHECKUNSYNCED_CHECKBOX");
			checkunsyncedCheckbox.IsChecked = () => debugSettings.SanityCheckUnsyncedCode;
			checkunsyncedCheckbox.OnClick = () => debugSettings.SanityCheckUnsyncedCode ^= true;

			// Video
			var windowModeDropdown = generalPane.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => SettingsMenuLogic.ShowWindowModeDropdown(windowModeDropdown, graphicsSettings);
			windowModeDropdown.GetText = () => graphicsSettings.Mode == WindowMode.Windowed ?
				"Windowed" : graphicsSettings.Mode == WindowMode.Fullscreen ? "Fullscreen" : "Pseudo-Fullscreen";

			var pixelDoubleCheckbox = generalPane.Get<CheckboxWidget>("PIXELDOUBLE_CHECKBOX");
			pixelDoubleCheckbox.IsChecked = () => graphicsSettings.PixelDouble;
			pixelDoubleCheckbox.OnClick = () =>
			{
				graphicsSettings.PixelDouble ^= true;
				Game.viewport.Zoom = graphicsSettings.PixelDouble ? 2 : 1;
			};

			var showShellmapCheckbox = generalPane.Get<CheckboxWidget>("SHOW_SHELLMAP");
			showShellmapCheckbox.IsChecked = () => gameSettings.ShowShellmap;
			showShellmapCheckbox.OnClick = () => gameSettings.ShowShellmap ^= true;

			generalPane.Get("WINDOW_RESOLUTION").IsVisible = () => graphicsSettings.Mode == WindowMode.Windowed;
			var windowWidth = generalPane.Get<TextFieldWidget>("WINDOW_WIDTH");
			windowWidth.Text = graphicsSettings.WindowedSize.X.ToString();

			var windowHeight = generalPane.Get<TextFieldWidget>("WINDOW_HEIGHT");
			windowHeight.Text = graphicsSettings.WindowedSize.Y.ToString();

			// Audio
			var soundSlider = generalPane.Get<SliderWidget>("SOUND_SLIDER");
			soundSlider.OnChange += x => { soundSettings.SoundVolume = x; Sound.SoundVolume = x;};
			soundSlider.Value = soundSettings.SoundVolume;

			var musicSlider = generalPane.Get<SliderWidget>("MUSIC_SLIDER");
			musicSlider.OnChange += x => { soundSettings.MusicVolume = x; Sound.MusicVolume = x; };
			musicSlider.Value = soundSettings.MusicVolume;

			var shellmapMusicCheckbox = generalPane.Get<CheckboxWidget>("SHELLMAP_MUSIC");
			shellmapMusicCheckbox.IsChecked = () => soundSettings.MapMusic;
			shellmapMusicCheckbox.OnClick = () => soundSettings.MapMusic ^= true;

			// Input pane
			var inputPane = panel.Get("INPUT_CONTROLS");
			inputPane.IsVisible = () => Settings == PanelType.Input;

			var inputButton = panel.Get<ButtonWidget>("INPUT_BUTTON");
			inputButton.OnClick = () => Settings = PanelType.Input;
			inputButton.IsHighlighted = () => Settings == PanelType.Input;

			var classicMouseCheckbox = inputPane.Get<CheckboxWidget>("CLASSICORDERS_CHECKBOX");
			classicMouseCheckbox.IsChecked = () => gameSettings.UseClassicMouseStyle;
			classicMouseCheckbox.OnClick = () => gameSettings.UseClassicMouseStyle ^= true;

			var scrollSlider = inputPane.Get<SliderWidget>("SCROLLSPEED_SLIDER");
			scrollSlider.Value = gameSettings.ViewportEdgeScrollStep;
			scrollSlider.OnChange += x => gameSettings.ViewportEdgeScrollStep = x;

			var edgescrollCheckbox = inputPane.Get<CheckboxWidget>("EDGESCROLL_CHECKBOX");
			edgescrollCheckbox.IsChecked = () => gameSettings.ViewportEdgeScroll;
			edgescrollCheckbox.OnClick = () => gameSettings.ViewportEdgeScroll ^= true;

			var mouseScrollDropdown = inputPane.Get<DropDownButtonWidget>("MOUSE_SCROLL");
			mouseScrollDropdown.OnMouseDown = _ => ShowMouseScrollDropdown(mouseScrollDropdown, gameSettings);
			mouseScrollDropdown.GetText = () => gameSettings.MouseScroll.ToString();

			var teamchatCheckbox = inputPane.Get<CheckboxWidget>("TEAMCHAT_CHECKBOX");
			teamchatCheckbox.IsChecked = () => gameSettings.TeamChatToggle;
			teamchatCheckbox.OnClick = () => gameSettings.TeamChatToggle ^= true;

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				playerSettings.Name = nameTextfield.Text;
				int x, y;
				int.TryParse(windowWidth.Text, out x);
				int.TryParse(windowHeight.Text, out y);
				graphicsSettings.WindowedSize = new int2(x,y);
				Game.Settings.Save();
				Ui.CloseWindow();
				onExit();
			};
		}

		bool ShowColorPicker(DropDownButtonWidget color, PlayerSettings s)
		{
			Action<ColorRamp> onSelect = c => {s.ColorRamp = c; color.RemovePanel();};
			Action<ColorRamp> onChange = c => {colorPreview.Ramp = c;};

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
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
			return true;
		}
	}
}
