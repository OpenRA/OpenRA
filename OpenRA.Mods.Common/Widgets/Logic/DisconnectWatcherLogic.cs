#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class DisconnectWatcherLogic : ChromeLogic
	{
		public class DisconnectWatcherLogicDynamicWidgets : DynamicWidgets
		{
			public override ISet<string> WindowWidgetIds { get; } = new HashSet<string>
			{
				"CONNECTIONFAILED_PANEL",
			};
			public override IReadOnlyDictionary<string, string> ParentWidgetIdForChildWidgetId { get; } = EmptyDictionary;
		}

		readonly DisconnectWatcherLogicDynamicWidgets dynamicWidgets = new();

		[ObjectCreator.UseCtor]
		public DisconnectWatcherLogic(Widget widget, World world, OrderManager orderManager)
		{
			var disconnected = false;
			widget.Get<LogicTickerWidget>("DISCONNECT_WATCHER").OnTick = () =>
			{
				if (orderManager.Connection is not NetworkConnection connection)
					return;

				if (disconnected || connection.ConnectionState != ConnectionState.NotConnected)
					return;

				Game.RunAfterTick(() => dynamicWidgets.OpenWindow("CONNECTIONFAILED_PANEL", new WidgetArgs
				{
					{ "password", CurrentServerSettings.Password },
					{ "connection", connection },
					{ "onAbort", null },
					{ "onQuit", () => IngameMenuLogic.OnQuit(world) },
					{ "onRetry", null }
				}));

				disconnected = true;
			};
		}
	}
}
