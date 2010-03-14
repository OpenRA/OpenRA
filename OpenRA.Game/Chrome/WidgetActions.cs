using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets.Actions
{
	public interface IWidgetAction { bool OnClick(MouseInput mi); }

	public class QuitButtonAction : IWidgetAction
	{
		public bool OnClick(MouseInput mi)
		{
			Game.Exit();	
			return true;
		}
	}
	
	public class JoinServerButtonAction : IWidgetAction
	{
		public bool OnClick(MouseInput mi)
		{
			Game.JoinServer(Game.Settings.NetworkHost, Game.Settings.NetworkPort);
			return true;
		}
	}
	
	public class OpenCreateServerMenuButtonAction : IWidgetAction
	{
		public bool OnClick(MouseInput mi)
		{
			WidgetLoader.rootWidget.GetWidget("MAINMENU_BG").Visible = false;
			WidgetLoader.rootWidget.GetWidget("CREATESERVER_BG").Visible = true;
			return true;
		}
	}
	
	public class CloseCreateServerMenuButtonAction : IWidgetAction
	{
		public bool OnClick(MouseInput mi)
		{
			WidgetLoader.rootWidget.GetWidget("MAINMENU_BG").Visible = true;
			WidgetLoader.rootWidget.GetWidget("CREATESERVER_BG").Visible = false;
			return true;
		}
	}
	
	public class CreateServerMenuButtonAction : IWidgetAction
	{
		public bool OnClick(MouseInput mi)
		{
			Game.CreateServer();
			return true;
		}
	}
}