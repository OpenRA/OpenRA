
using System;

namespace OpenRA.Widgets.Delegates
{
	public class LobbyDelegate : IWidgetDelegate
	{
		public LobbyDelegate ()
		{
			var r = Chrome.rootWidget;
			var lobby = r.GetWidget("SERVER_LOBBY");
			var mapButton = lobby.GetWidget("CHANGEMAP_BUTTON");
			mapButton.OnMouseUp = mi => {
				r.OpenWindow("MAP_CHOOSER");
				return true;
			};
			mapButton.IsVisible = () => {return (mapButton.Visible && Game.IsHost);};
		}
	}
}
