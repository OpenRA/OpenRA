#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class DisconnectWatcherLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public DisconnectWatcherLogic(Widget widget, OrderManager orderManager)
		{
			var disconnected = false;
			widget.Get<LogicTickerWidget>("DISCONNECT_WATCHER").OnTick = () =>
			{
				if (disconnected || orderManager.Connection.ConnectionState != ConnectionState.NotConnected)
					return;

				Game.RunAfterTick(() => Ui.OpenWindow("CONNECTIONFAILED_PANEL", new WidgetArgs {
					{ "orderManager", orderManager },
					{ "onAbort", null },
					{ "onRetry", null }
				}));

				disconnected = true;
			};
		}
	}
}
