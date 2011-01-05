#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class SettingsMenuDelegate : IWidgetDelegate
	{
		Widget bg;
		public SettingsMenuDelegate()
		{
			bg = Widget.RootWidget.GetWidget<BackgroundWidget>("SETTINGS_MENU");
			var tabs = bg.GetWidget<ContainerWidget>("TAB_CONTAINER");
			
			//Tabs
			tabs.GetWidget<ButtonWidget>("GENERAL").OnMouseUp = mi => FlipToTab("GENERAL_PANE");
			tabs.GetWidget<ButtonWidget>("AUDIO").OnMouseUp = mi => FlipToTab("AUDIO_PANE");
			tabs.GetWidget<ButtonWidget>("DISPLAY").OnMouseUp = mi => FlipToTab("DISPLAY_PANE");
			tabs.GetWidget<ButtonWidget>("DEBUG").OnMouseUp = mi => FlipToTab("DEBUG_PANE");
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

			general.GetWidget<CheckboxWidget>("EDGE_SCROLL").Bind(Game.Settings.Game, "ViewportEdgeScroll");
            
            // Added scroll sensitivity - Gecko	
            var edgeScrollSlider = general.GetWidget<SliderWidget>("EDGE_SCROLL_AMOUNT");
            if (edgeScrollSlider != null) // Backwards compatible - Gecko
			{
				edgeScrollSlider.SetOffset(Game.Settings.Game.ViewportEdgeScrollStep);
                edgeScrollSlider.OnChange += _ => { Game.Settings.Game.ViewportEdgeScrollStep = edgeScrollSlider.GetOffset(); };
				Game.Settings.Game.ViewportEdgeScrollStep = edgeScrollSlider.GetOffset();
            }

			general.GetWidget<CheckboxWidget>("INVERSE_SCROLL").Bind(Game.Settings.Game, "InverseDragScroll");
			general.GetWidget<CheckboxWidget>("TEAMCHAT_TOGGLE").Bind(Game.Settings.Game, "TeamChatToggle");

			
			// Audio
			var audio = bg.GetWidget("AUDIO_PANE");
			
			var soundslider = audio.GetWidget<SliderWidget>("SOUND_VOLUME");
			soundslider.OnChange += x => { Sound.SoundVolume = x; };
			soundslider.GetOffset = () => { return Sound.SoundVolume; };
			soundslider.SetOffset(Sound.SoundVolume);
			
			var musicslider = audio.GetWidget<SliderWidget>("MUSIC_VOLUME");
			musicslider.OnChange += x => { Sound.MusicVolume = x; };
			musicslider.GetOffset = () => { return Sound.MusicVolume; };
			musicslider.SetOffset(Sound.MusicVolume);
			
			
			// Display
			var display = bg.GetWidget("DISPLAY_PANE");
			display.GetWidget<CheckboxWidget>("FULLSCREEN_CHECKBOX").Bind(Game.Settings.Game, "TeamChatToggle");

			var fullscreen = display.GetWidget<CheckboxWidget>("FULLSCREEN_CHECKBOX");
			fullscreen.IsChecked = () => Game.Settings.Graphics.Mode != WindowMode.Windowed;
			fullscreen.OnChange += c => Game.Settings.Graphics.Mode = (Game.Settings.Graphics.Mode == WindowMode.Windowed) ? WindowMode.PseudoFullscreen : WindowMode.Windowed;
	
			var width = display.GetWidget<TextFieldWidget>("SCREEN_WIDTH");
			Game.Settings.Graphics.WindowedSize.X = (Game.Settings.Graphics.WindowedSize.X < Game.Settings.Graphics.MinResolution.X)?
				Game.Settings.Graphics.MinResolution.X : Game.Settings.Graphics.WindowedSize.X;
			width.Text = Game.Settings.Graphics.WindowedSize.X.ToString();
			width.OnLoseFocus = () =>
			{
				try {
					var w = int.Parse(width.Text);
					if (w > Game.Settings.Graphics.MinResolution.X)
						Game.Settings.Graphics.WindowedSize = new int2(w, Game.Settings.Graphics.WindowedSize.Y);
				}
				catch (FormatException) {
					width.Text = Game.Settings.Graphics.WindowedSize.X.ToString();
				}
			};
			width.OnEnterKey = () => { width.LoseFocus(); return true; };
			
			var height = display.GetWidget<TextFieldWidget>("SCREEN_HEIGHT");
			Game.Settings.Graphics.WindowedSize.Y = (Game.Settings.Graphics.WindowedSize.Y < Game.Settings.Graphics.MinResolution.Y)?
				Game.Settings.Graphics.MinResolution.Y : Game.Settings.Graphics.WindowedSize.Y;
			height.Text = Game.Settings.Graphics.WindowedSize.Y.ToString();
			height.OnLoseFocus = () =>
			{
				try {
					var h = int.Parse(height.Text);
					if (h > Game.Settings.Graphics.MinResolution.Y)
						Game.Settings.Graphics.WindowedSize = new int2(Game.Settings.Graphics.WindowedSize.X, h);	
					else 
						height.Text = Game.Settings.Graphics.WindowedSize.Y.ToString();
				}
				catch (FormatException) {
					height.Text = Game.Settings.Graphics.WindowedSize.Y.ToString();
				}
			};
			height.OnEnterKey = () => { height.LoseFocus(); return true; };
			
			// Debug
			var debug = bg.GetWidget("DEBUG_PANE");
			debug.GetWidget<CheckboxWidget>("PERFDEBUG_CHECKBOX").Bind(Game.Settings.Debug, "PerfGraph");
			debug.GetWidget<CheckboxWidget>("GAMETIME_CHECKBOX").Bind(Game.Settings.Game, "MatchTimer");

			bg.GetWidget("BUTTON_CLOSE").OnMouseUp = mi => {
				Game.Settings.Save();
				Widget.CloseWindow();
				return true;
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
