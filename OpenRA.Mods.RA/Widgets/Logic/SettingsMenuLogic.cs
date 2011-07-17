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
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class SettingsMenuLogic
	{
		Widget bg;
		public SettingsMenuLogic()
		{
			bg = Widget.RootWidget.GetWidget<BackgroundWidget>("SETTINGS_MENU");
			var tabs = bg.GetWidget<ContainerWidget>("TAB_CONTAINER");
			
			//Tabs
			tabs.GetWidget<ButtonWidget>("GENERAL").OnClick = () => FlipToTab("GENERAL_PANE");
			tabs.GetWidget<ButtonWidget>("AUDIO").OnClick = () => FlipToTab("AUDIO_PANE");
			tabs.GetWidget<ButtonWidget>("DISPLAY").OnClick = () => FlipToTab("DISPLAY_PANE");
			tabs.GetWidget<ButtonWidget>("DEBUG").OnClick = () => FlipToTab("DEBUG_PANE");
			FlipToTab("GENERAL_PANE");
			
			//General
			var general = bg.GetWidget("GENERAL_PANE");
			
			var name = general.GetWidget<TextFieldWidget>("NAME");
			name.Text = Game.Settings.Player.Name;
			name.OnLoseFocus = () =>
			{
				name.Text = name.Text.Trim();
		
				if (name.Text.Length == 0)
					name.Text = Game.Settings.Player.Name;
				else
					Game.Settings.Player.Name = name.Text;
			};
            name.OnEnterKey = () => { name.LoseFocus(); return true; };
			
			var edgescrollCheckbox = general.GetWidget<CheckboxWidget>("EDGE_SCROLL");
			edgescrollCheckbox.IsChecked = () => Game.Settings.Game.ViewportEdgeScroll;
			edgescrollCheckbox.OnClick = () => Game.Settings.Game.ViewportEdgeScroll ^= true;
			
            var edgeScrollSlider = general.GetWidget<SliderWidget>("EDGE_SCROLL_AMOUNT");
			edgeScrollSlider.Value = Game.Settings.Game.ViewportEdgeScrollStep;
            edgeScrollSlider.OnChange += x => Game.Settings.Game.ViewportEdgeScrollStep = x;

			var inversescroll = general.GetWidget<CheckboxWidget>("INVERSE_SCROLL");
			inversescroll.IsChecked = () => Game.Settings.Game.MouseScroll == MouseScrollType.Inverted;
			inversescroll.OnClick = () => Game.Settings.Game.MouseScroll = (Game.Settings.Game.MouseScroll == MouseScrollType.Inverted) ? MouseScrollType.Standard : MouseScrollType.Inverted;
	
			var teamchatCheckbox = general.GetWidget<CheckboxWidget>("TEAMCHAT_TOGGLE");
			teamchatCheckbox.IsChecked = () => Game.Settings.Game.TeamChatToggle;
			teamchatCheckbox.OnClick = () => Game.Settings.Game.TeamChatToggle ^= true;
			
			// Audio
			var audio = bg.GetWidget("AUDIO_PANE");
			
			var soundslider = audio.GetWidget<SliderWidget>("SOUND_VOLUME");
			soundslider.OnChange += x => Sound.SoundVolume = x;
			soundslider.Value = Sound.SoundVolume;
			
			var musicslider = audio.GetWidget<SliderWidget>("MUSIC_VOLUME");
			musicslider.OnChange += x => Sound.MusicVolume = x;
			musicslider.Value = Sound.MusicVolume;
			
			// Display
			var display = bg.GetWidget("DISPLAY_PANE");
			var gs = Game.Settings.Graphics;

			var fullscreen = display.GetWidget<CheckboxWidget>("FULLSCREEN_CHECKBOX");
			fullscreen.IsChecked = () => gs.Mode != WindowMode.Windowed;
			fullscreen.OnClick = () => gs.Mode = (gs.Mode == WindowMode.Windowed) ? WindowMode.PseudoFullscreen : WindowMode.Windowed;
	
			var width = display.GetWidget<TextFieldWidget>("SCREEN_WIDTH");
			gs.WindowedSize.X = Math.Max(gs.WindowedSize.X, gs.MinResolution.X);
			width.Text = gs.WindowedSize.X.ToString();
			width.OnLoseFocus = () =>
			{
				try {
					var w = int.Parse(width.Text);
					if (w > gs.MinResolution.X)
						gs.WindowedSize = new int2(w, gs.WindowedSize.Y);
				}
				catch (FormatException) {
					width.Text = gs.WindowedSize.X.ToString();
				}
			};
			width.OnEnterKey = () => { width.LoseFocus(); return true; };
			
			var height = display.GetWidget<TextFieldWidget>("SCREEN_HEIGHT");
			gs.WindowedSize.Y = Math.Max(gs.WindowedSize.Y, gs.MinResolution.Y);
			height.Text = gs.WindowedSize.Y.ToString();
			height.OnLoseFocus = () =>
			{
				try {
					var h = int.Parse(height.Text);
					if (h > gs.MinResolution.Y)
						gs.WindowedSize = new int2(gs.WindowedSize.X, h);	
					else 
						height.Text = gs.WindowedSize.Y.ToString();
				}
				catch (FormatException) {
					height.Text = gs.WindowedSize.Y.ToString();
				}
			};
			height.OnEnterKey = () => { height.LoseFocus(); return true; };
			
			// Debug
			var debug = bg.GetWidget("DEBUG_PANE");

			var perfgraphCheckbox = debug.GetWidget<CheckboxWidget>("PERFDEBUG_CHECKBOX");
			perfgraphCheckbox.IsChecked = () => Game.Settings.Debug.PerfGraph;
			perfgraphCheckbox.OnClick = () => Game.Settings.Debug.PerfGraph ^= true;
			
			Game.Settings.Game.MatchTimer = true;
			
			var checkunsyncedCheckbox = debug.GetWidget<CheckboxWidget>("CHECKUNSYNCED_CHECKBOX");
			checkunsyncedCheckbox.IsChecked = () => Game.Settings.Debug.SanityCheckUnsyncedCode;
			checkunsyncedCheckbox.OnClick = () => Game.Settings.Debug.SanityCheckUnsyncedCode ^= true;
	
			bg.GetWidget<ButtonWidget>("BUTTON_CLOSE").OnClick = () => {
				Game.Settings.Save();
				Widget.CloseWindow();
			};
		}
		
		string open = null;
		bool FlipToTab(string id)
		{
			if (open != null)
				bg.GetWidget(open).Visible = false;
			
			open = id;
			bg.GetWidget(open).Visible = true;
			return true;
		}
	}
}
