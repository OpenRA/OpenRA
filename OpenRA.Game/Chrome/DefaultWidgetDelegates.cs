using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Server;

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
	
	public class ServerBrowserDelegate : IWidgetDelegate
	{
		IEnumerable<GameServer> GameList;
		
		public bool OnClick(Widget w, MouseInput mi)
		{
			// Main Menu root
			if (w.Id == "MAINMENU_BUTTON_JOIN")
			{
				WidgetLoader.rootWidget.GetWidget("MAINMENU_BG").Visible = false;
				Widget bg = 	WidgetLoader.rootWidget.GetWidget("JOINSERVER_BG");
				bg.Visible = true;
				
				int height = 50;
				int width = 300;
				int i = 0;
				GameList = MasterServerQuery.GetGameList(Game.Settings.MasterServer);
				
				//bg.Children.Clear();

				foreach (var game in GameList)
				{
					ButtonWidget b = new ButtonWidget();
					b.Bounds = new Rectangle(bg.Bounds.X + 20, bg.Bounds.Y + height, width, 25);	
					b.GetType().GetField("Id").SetValue( b, "JOIN_GAME_{0}".F(i));
					b.GetType().GetField("Text").SetValue( b, "{0} ({1})".F(game.Name, game.Address));
					b.GetType().GetField("Delegate").SetValue( b, "ServerBrowserDelegate");
				
					b.EventBounds = b.Bounds;
					bg.AddChild(b);
					
					height += 35;
				}
				
				return true;
			}
			
			if (w.Id == "JOINSERVER_BUTTON_DIRECTCONNECT")
			{
				Game.JoinServer(Game.Settings.NetworkHost, Game.Settings.NetworkPort);
				return true;
			}
			
			if (w.Id.Substring(0,10) == "JOIN_GAME_")
			{
				int index = int.Parse(w.Id.Substring(10));
				GameList = MasterServerQuery.GetGameList(Game.Settings.MasterServer);
				var game = GameList.ElementAt(index);
				Game.JoinServer(game.Address.Split(':')[0], int.Parse(game.Address.Split(':')[1]));
				return true;
			}			
				
			if (w.Id == "JOINSERVER_BUTTON_CANCEL")
			{
				WidgetLoader.rootWidget.GetWidget("JOINSERVER_BG").Visible = false;
				WidgetLoader.rootWidget.GetWidget("MAINMENU_BG").Visible = true;
				return true;
			}
			
			return false;
		}
	}
}