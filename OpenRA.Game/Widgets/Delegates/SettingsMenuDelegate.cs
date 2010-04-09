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
				Game.Settings.UnitDebug = !Game.Settings.UnitDebug;
				return true;
			};
			
			r.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_PATHDEBUG").Checked = () => {return Game.Settings.PathDebug;};
			r.GetWidget("SETTINGS_CHECKBOX_PATHDEBUG").OnMouseDown = mi => {
				Game.Settings.PathDebug = !Game.Settings.PathDebug;
				return true;
			};
			
			r.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_INDEXDEBUG").Checked = () => {return Game.Settings.IndexDebug;};
			r.GetWidget("SETTINGS_CHECKBOX_INDEXDEBUG").OnMouseDown = mi => {
				Game.Settings.IndexDebug = !Game.Settings.IndexDebug;
				return true;
			};
			
			r.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_PERFGRAPH").Checked = () => {return Game.Settings.PerfGraph;};
			r.GetWidget("SETTINGS_CHECKBOX_PERFGRAPH").OnMouseDown = mi => {
				Game.Settings.PerfGraph = !Game.Settings.PerfGraph;
				return true;
			};
			
			r.GetWidget<CheckboxWidget>("SETTINGS_CHECKBOX_PERFTEXT").Checked = () => {return Game.Settings.PerfText;};
			r.GetWidget("SETTINGS_CHECKBOX_PERFTEXT").OnMouseDown = mi => {
				Game.Settings.PerfText = !Game.Settings.PerfText;
				return true;
			};
			
			
			// Menu Buttons
			r.GetWidget("MAINMENU_BUTTON_SETTINGS").OnMouseUp = mi => {
				r.ShowMenu("SETTINGS_BG");
				return true;
			};
			
			r.GetWidget("SETTINGS_BUTTON_OK").OnMouseUp = mi => {
				r.ShowMenu("MAINMENU_BG");
				return true;
			};
		}
	}
}
