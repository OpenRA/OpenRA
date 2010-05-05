#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

namespace OpenRA.Widgets.Delegates
{
	public class ConnectionDialogsDelegate : IWidgetDelegate
	{
		public ConnectionDialogsDelegate()
		{
			var r = Chrome.rootWidget;
			r.GetWidget("CONNECTION_BUTTON_ABORT").OnMouseUp = mi => {
				r.GetWidget("CONNECTION_BUTTON_ABORT").Parent.Visible = false;
				Game.Disconnect();
				return true;
			};
			r.GetWidget("CONNECTION_BUTTON_CANCEL").OnMouseUp = mi => {
				r.GetWidget("CONNECTION_BUTTON_CANCEL").Parent.Visible = false;
				Game.Disconnect();
				return true;
			};
			r.GetWidget("CONNECTION_BUTTON_RETRY").OnMouseUp = mi => {
				Game.JoinServer(Game.CurrentHost, Game.CurrentPort);
				return true;
			};

			r.GetWidget<LabelWidget>("CONNECTING_DESC").GetText = () =>
				"Connecting to {0}:{1}...".F(Game.CurrentHost, Game.CurrentPort);

			r.GetWidget<LabelWidget>("CONNECTION_FAILED_DESC").GetText = () =>
				"Could not connect to {0}:{1}".F(Game.CurrentHost, Game.CurrentPort);
		}
	}
}
