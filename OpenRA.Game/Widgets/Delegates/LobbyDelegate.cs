
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenRA.Widgets.Delegates
{
	public class LobbyDelegate : IWidgetDelegate
	{
		Widget PlayerTemplate;
		Widget Players;
		
		public LobbyDelegate ()
		{
			var r = Chrome.rootWidget;
			var lobby = r.GetWidget("SERVER_LOBBY");
			Players = Chrome.rootWidget.GetWidget("SERVER_LOBBY").GetWidget("PLAYERS");
			PlayerTemplate = Players.GetWidget("TEMPLATE");
			
			
			var mapButton = lobby.GetWidget("CHANGEMAP_BUTTON");
			mapButton.OnMouseUp = mi => {
				r.OpenWindow("MAP_CHOOSER");
				return true;
			};
			mapButton.IsVisible = () => {return (mapButton.Visible && Game.IsHost);};
			
			Game.LobbyInfoChanged += () => { UpdatePlayerList(); };
			
			UpdatePlayerList();
		}
		
		void UpdatePlayerList()
		{
			Log.Write("UpdatePlayerList");
			
			Players.Children.Clear();
			int i = 0;
			foreach(var client in Game.LobbyInfo.Clients)
			{
				Log.Write("Client {0}",client.Name);
				var template = PlayerTemplate.Clone();
				template.Id = "PLAYER_{0}".F(client.Name);
				template.GetWidget<ButtonWidget>("NAME").GetText = () => client.Name;
				template.Bounds = new Rectangle(template.Bounds.X, template.Bounds.Y + i, template.Bounds.Width, template.Bounds.Height); 
				template.Visible = true;
				Players.AddChild(template);
				i += 30;
			}
			Log.Write("Players has {0} children",Players.Children.Count);
			foreach (var foo in Players.Children)
				Log.Write("{0} {1} {2}",foo.Id, foo.GetWidget<ButtonWidget>("NAME").GetText(), foo.Bounds.Y);
		}
	}
}
