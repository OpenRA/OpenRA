using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets.Delegates
{
	public interface IWidgetDelegate { bool OnClick(Widget w, MouseInput mi); }

	public class MainMenuButtonsDelegate : IWidgetDelegate
	{
		public bool OnClick(Widget w, MouseInput mi)
		{
			// Main Menu root
			if (w.Id == "MAINMENU_BUTTON_QUIT")
			{
				Game.Exit();	
				return true;
			}
			
			if (w.Id == "MAINMENU_BUTTON_JOIN")
			{
				Game.JoinServer(Game.Settings.NetworkHost, Game.Settings.NetworkPort);
				return true;
			}
			
			if (w.Id == "MAINMENU_BUTTON_CREATE")
			{
				WidgetLoader.rootWidget.GetWidget("MAINMENU_BG").Visible = false;
				WidgetLoader.rootWidget.GetWidget("CREATESERVER_BG").Visible = true;
				return true;
			}
			
			// "Create Server" submenu
			if (w.Id == "CREATESERVER_BUTTON_CANCEL")
			{
				WidgetLoader.rootWidget.GetWidget("MAINMENU_BG").Visible = true;
				WidgetLoader.rootWidget.GetWidget("CREATESERVER_BG").Visible = false;
				return true;
			}
			
			if (w.Id == "CREATESERVER_BUTTON_START")
			{
				Game.CreateServer();
				return true;
			}
			
			return false;
		}
	}
}