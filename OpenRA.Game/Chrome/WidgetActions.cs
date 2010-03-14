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
	
	public class CreateServerButtonAction : IWidgetAction
	{
		public bool OnClick(MouseInput mi)
		{
			Game.CreateServer();
			return true;
		}
	}
}