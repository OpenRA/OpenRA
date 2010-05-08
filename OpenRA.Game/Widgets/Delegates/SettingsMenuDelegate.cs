using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Widgets.Delegates
{
	public class SettingsMenuDelegate : IWidgetDelegate
	{
		public SettingsMenuDelegate()
		{
			var r = Chrome.rootWidget;
			
			// Checkboxes		
			r.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_UNITDEBUG").Checked = () => {return Game.Settings.UnitDebug;};
			r.GetWidget("SETTINGS_CHECKBOX_UNITDEBUG").OnMouseDown = mi => {
				Game.Settings.UnitDebug ^= true;
				return true;
			};
			
			r.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_PATHDEBUG").Checked = () => {return Game.Settings.PathDebug;};
			r.GetWidget("SETTINGS_CHECKBOX_PATHDEBUG").OnMouseDown = mi => {
				Game.Settings.PathDebug ^= true;
				return true;
			};
			
			r.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_INDEXDEBUG").Checked = () => {return Game.Settings.IndexDebug;};
			r.GetWidget("SETTINGS_CHECKBOX_INDEXDEBUG").OnMouseDown = mi => {
				Game.Settings.IndexDebug ^= true;
				return true;
			};
			
			r.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_PERFDEBUG").Checked = () => {return Game.Settings.PerfDebug;};
			r.GetWidget("SETTINGS_CHECKBOX_PERFDEBUG").OnMouseDown = mi => {
				Game.Settings.PerfDebug ^= true;
				return true;
			};

			r.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_SYNCREPORTS").Checked = () => { return Game.Settings.RecordSyncReports; };
			r.GetWidget("SETTINGS_CHECKBOX_SYNCREPORTS").OnMouseDown = mi =>
			{
				Game.Settings.RecordSyncReports ^= true;
				return true;
			};
			
			r.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_MUSICPLAYER").Checked = () => { return Game.Settings.MusicPlayer; };
			r.GetWidget("SETTINGS_CHECKBOX_MUSICPLAYER").OnMouseDown = mi =>
			{
				Game.Settings.MusicPlayer ^= true;
				r.GetWidget("MUSIC_BG").Visible = Game.Settings.MusicPlayer;
				return true;
			};
						
			// Menu Buttons
			r.GetWidget("MAINMENU_BUTTON_SETTINGS").OnMouseUp = mi => {
				r.OpenWindow("SETTINGS_BG");
				return true;
			};
			
			r.GetWidget("SETTINGS_BUTTON_OK").OnMouseUp = mi => {
				r.CloseWindow();
				return true;
			};
		}
	}
}
