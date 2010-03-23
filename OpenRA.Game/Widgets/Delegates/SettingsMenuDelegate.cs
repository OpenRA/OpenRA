using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Widgets.Delegates
{
	public class SettingsMenuDelegate : WidgetDelegate
	{
		public override bool GetState(Widget w)
		{
			if (w.Id == "SETTINGS_CHECKBOX_UNITDEBUG") return Game.Settings.UnitDebug;
			if (w.Id == "SETTINGS_CHECKBOX_PATHDEBUG") return Game.Settings.PathDebug;
			if (w.Id == "SETTINGS_CHECKBOX_INDEXDEBUG") return Game.Settings.IndexDebug;
			if (w.Id == "SETTINGS_CHECKBOX_PERFGRAPH") return Game.Settings.PerfGraph;
			if (w.Id == "SETTINGS_CHECKBOX_PERFTEXT") return Game.Settings.PerfText;
			return false;
		}

		public override bool OnMouseDown(Widget w, MouseInput mi)
		{
			if (w.Id == "SETTINGS_CHECKBOX_UNITDEBUG")
			{
				Game.Settings.UnitDebug = !Game.Settings.UnitDebug;
				return true;
			}

			if (w.Id == "SETTINGS_CHECKBOX_PATHDEBUG")
			{
				Game.Settings.PathDebug = !Game.Settings.PathDebug;
				return true;
			}

			if (w.Id == "SETTINGS_CHECKBOX_INDEXDEBUG")
			{
				Game.Settings.IndexDebug = !Game.Settings.IndexDebug;
				return true;
			}

			if (w.Id == "SETTINGS_CHECKBOX_PERFGRAPH")
			{
				Game.Settings.PerfGraph = !Game.Settings.PerfGraph;
				return true;
			}

			if (w.Id == "SETTINGS_CHECKBOX_PERFTEXT")
			{
				Game.Settings.PerfText = !Game.Settings.PerfText;
				return true;
			}

			return false;
		}

		public override bool OnMouseUp(Widget w, MouseInput mi)
		{
			if (w.Id == "MAINMENU_BUTTON_SETTINGS")
			{
				Game.chrome.rootWidget.ShowMenu("SETTINGS_BG");
				return true;
			}

			if (w.Id == "SETTINGS_BUTTON_OK")
			{
				Game.chrome.rootWidget.ShowMenu("MAINMENU_BG");
				return true;
			}

			return false;
		}
	}
}
