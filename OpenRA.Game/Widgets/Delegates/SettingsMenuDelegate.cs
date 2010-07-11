using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats.Graphics;

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
			
			
			// Audio
			var audio = bg.GetWidget("AUDIO_PANE");
			var music = audio.GetWidget<CheckboxWidget>("MUSICPLAYER_CHECKBOX");
			
			music.Checked = () => { return Game.Settings.MusicPlayer; };
			music.OnMouseDown = mi =>
			{
				Game.Settings.MusicPlayer ^= true;
				Chrome.rootWidget.GetWidget("MUSIC_BG").Visible = Game.Settings.MusicPlayer;
				Game.Settings.Save();
				return true;
			};
			
			// Display
			var display = bg.GetWidget("DISPLAY_PANE");
			var fullscreen = display.GetWidget<CheckboxWidget>("FULLSCREEN_CHECKBOX");
			fullscreen.Checked = () => {return Game.Settings.WindowMode != WindowMode.Windowed;};
			fullscreen.OnMouseDown = mi =>
			{
				Game.Settings.WindowMode = (Game.Settings.WindowMode == WindowMode.Windowed) ? WindowMode.PseudoFullscreen : WindowMode.Windowed;
				Game.Settings.Save();
				return true;
			};
			
			// Debug
			var debug = bg.GetWidget("DEBUG_PANE");
			var perfdebug = debug.GetWidget<CheckboxWidget>("PERFDEBUG_CHECKBOX");
			perfdebug.Checked = () => {return Game.Settings.PerfDebug;};
			perfdebug.OnMouseDown = mi =>
			{
				Game.Settings.PerfDebug ^= true;
				Game.Settings.Save();
				return true;
			};
			
			var syncreports = debug.GetWidget<CheckboxWidget>("SYNCREPORTS_CHECKBOX");
			syncreports.Checked = () => { return Game.Settings.RecordSyncReports; };
			syncreports.OnMouseDown = mi =>
			{
				Game.Settings.RecordSyncReports ^= true;
				Game.Settings.Save();
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
			
			bg.GetWidget("BUTTON_CLOSE").OnMouseUp = mi => {
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
