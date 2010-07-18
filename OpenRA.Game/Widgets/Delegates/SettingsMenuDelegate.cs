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
using System.Windows.Forms;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;

namespace OpenRA.Widgets.Delegates
{
	public class SettingsMenuDelegate : IWidgetDelegate
	{
		Widget bg;
		public SettingsMenuDelegate()
		{
			bg = Chrome.rootWidget.GetWidget<BackgroundWidget>("SETTINGS_MENU");
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
			name.Text = Game.Settings.PlayerName;
			name.OnLoseFocus = () =>
			{
				name.Text = name.Text.Trim();
		
				if (name.Text.Length == 0)
					name.Text = Game.Settings.PlayerName;
				else
					Game.Settings.PlayerName = name.Text;
			};
			name.OnEnterKey = () => { name.LoseFocus(); return true; };
			
			// Audio
			var audio = bg.GetWidget("AUDIO_PANE");
			
			var soundslider = audio.GetWidget<SliderWidget>("SOUND_VOLUME");
			soundslider.OnChange += x => { Sound.SoundVolume = x; };
			soundslider.GetOffset = () => { return Sound.SoundVolume; };
			
			var musicslider = audio.GetWidget<SliderWidget>("MUSIC_VOLUME");
			musicslider.OnChange += x => { Sound.MusicVolume = x; };
			musicslider.GetOffset = () => { return Sound.MusicVolume; };
			
			var music = audio.GetWidget<CheckboxWidget>("MUSICPLAYER_CHECKBOX");
			music.Checked = () => { return Game.Settings.MusicPlayer; };
			music.OnMouseDown = mi =>
			{
				Game.Settings.MusicPlayer ^= true;
				Chrome.rootWidget.GetWidget("MUSIC_BG").Visible = Game.Settings.MusicPlayer;
				return true;
			};
			
			// Display
			var display = bg.GetWidget("DISPLAY_PANE");
			var fullscreen = display.GetWidget<CheckboxWidget>("FULLSCREEN_CHECKBOX");
			fullscreen.Checked = () => {return Game.Settings.WindowMode != WindowMode.Windowed;};
			fullscreen.OnMouseDown = mi =>
			{
				Game.Settings.WindowMode = (Game.Settings.WindowMode == WindowMode.Windowed) ? WindowMode.PseudoFullscreen : WindowMode.Windowed;
				return true;
			};
			
			var width = display.GetWidget<TextFieldWidget>("SCREEN_WIDTH");
			Game.Settings.WindowedSize.X = (Game.Settings.WindowedSize.X < UserSettings.MinResolution.X)?
				UserSettings.MinResolution.X : Game.Settings.WindowedSize.X;
			width.Text = Game.Settings.WindowedSize.X.ToString();
			width.OnLoseFocus = () =>
			{
				try {
					var w = int.Parse(width.Text);
					if (w > UserSettings.MinResolution.X && w <= Screen.PrimaryScreen.Bounds.Size.Width)
						Game.Settings.WindowedSize = new int2(w, Game.Settings.WindowedSize.Y);
					else
						width.Text = Game.Settings.WindowedSize.X.ToString();
				}
				catch (FormatException) {
					width.Text = Game.Settings.WindowedSize.X.ToString();
				}
			};
			width.OnEnterKey = () => { width.LoseFocus(); return true; };
			
			var height = display.GetWidget<TextFieldWidget>("SCREEN_HEIGHT");
			Game.Settings.WindowedSize.Y = (Game.Settings.WindowedSize.Y < UserSettings.MinResolution.Y)?
				UserSettings.MinResolution.Y : Game.Settings.WindowedSize.Y;
			height.Text = Game.Settings.WindowedSize.Y.ToString();
			height.OnLoseFocus = () =>
			{
				try {
					var h = int.Parse(height.Text);
					if (h > UserSettings.MinResolution.Y  && h <= Screen.PrimaryScreen.Bounds.Size.Height)
						Game.Settings.WindowedSize = new int2(Game.Settings.WindowedSize.X, h);	
					else 
						height.Text = Game.Settings.WindowedSize.Y.ToString();
				}
				catch (FormatException) {
					height.Text = Game.Settings.WindowedSize.Y.ToString();
				}
			};
			height.OnEnterKey = () => { height.LoseFocus(); return true; };
			
			// Debug
			var debug = bg.GetWidget("DEBUG_PANE");
			var perfdebug = debug.GetWidget<CheckboxWidget>("PERFDEBUG_CHECKBOX");
			perfdebug.Checked = () => {return Game.Settings.PerfDebug;};
			perfdebug.OnMouseDown = mi =>
			{
				Game.Settings.PerfDebug ^= true;
				return true;
			};
			
			var syncreports = debug.GetWidget<CheckboxWidget>("SYNCREPORTS_CHECKBOX");
			syncreports.Checked = () => { return Game.Settings.RecordSyncReports; };
			syncreports.OnMouseDown = mi =>
			{
				Game.Settings.RecordSyncReports ^= true;
				return true;
			};
			
			var unitdebug = debug.GetWidget<CheckboxWidget>("UNITDEBUG_CHECKBOX");
			unitdebug.Checked = () => {return Game.Settings.UnitDebug;};
			unitdebug.OnMouseDown = mi => 
			{
				Game.Settings.UnitDebug ^= true;
				return true;
			};
			
			var pathdebug = debug.GetWidget<CheckboxWidget>("PATHDEBUG_CHECKBOX");
			pathdebug.Checked = () => {return Game.Settings.PathDebug;};
			pathdebug.OnMouseDown = mi => 
			{
				Game.Settings.PathDebug ^= true;
				return true;
			};
			
			var indexdebug = debug.GetWidget<CheckboxWidget>("INDEXDEBUG_CHECKBOX");
			indexdebug.Checked = () => {return Game.Settings.IndexDebug;};
			indexdebug.OnMouseDown = mi => 
			{
				Game.Settings.IndexDebug ^= true;
				return true;
			};
			
			var timedebug = debug.GetWidget<CheckboxWidget>("GAMETIME_CHECKBOX");
			timedebug.Checked = () => {return Game.Settings.ShowGameTimer;};
			timedebug.OnMouseDown = mi => 
			{
				Game.Settings.ShowGameTimer ^= true;
				return true;
			};
			
			bg.GetWidget("BUTTON_CLOSE").OnMouseUp = mi => {
				Game.Settings.Save();
				Chrome.rootWidget.CloseWindow();
				return true;
			};
			
			// Menu Buttons
			Chrome.rootWidget.GetWidget("MAINMENU_BUTTON_SETTINGS").OnMouseUp = mi => {
				Chrome.rootWidget.OpenWindow("SETTINGS_MENU");
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
