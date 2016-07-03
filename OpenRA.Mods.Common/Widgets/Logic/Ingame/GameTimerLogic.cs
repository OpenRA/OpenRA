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

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class GameTimerLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public GameTimerLogic(Widget widget, OrderManager orderManager, World world)
		{
			var timer = widget.GetOrNull<LabelWidget>("GAME_TIMER");
			var status = widget.GetOrNull<LabelWidget>("GAME_TIMER_STATUS");
			var startTick = Ui.LastTickTime;

			Func<bool> shouldShowStatus = () => (world.Paused || world.Timestep != world.LobbyInfo.GlobalSettings.Timestep)
				&& (Ui.LastTickTime - startTick) / 1000 % 2 == 0;

			Func<string> statusText = () =>
			{
				if (world.Paused || world.Timestep == 0)
					return "Paused";

				if (world.Timestep == 1)
					return "Max Speed";

				return "{0}% Speed".F(world.LobbyInfo.GlobalSettings.Timestep * 100 / world.Timestep);
			};

			if (timer != null)
			{
				// Timers in replays should be synced to the effective game time, not the playback time.
				var timestep = world.Timestep;
				if (world.IsReplay)
					timestep = world.WorldActor.Trait<MapOptions>().GameSpeed.Timestep;

				timer.GetText = () =>
				{
					if (status == null && shouldShowStatus())
						return statusText();

					return WidgetUtils.FormatTime(world.WorldTick, timestep);
				};
			}

			if (status != null)
			{
				// Blink the status line
				status.IsVisible = shouldShowStatus;
				status.GetText = statusText;
			}

			var percentage = widget.GetOrNull<LabelWidget>("GAME_TIMER_PERCENTAGE");
			if (percentage != null)
			{
				var connection = orderManager.Connection as ReplayConnection;
				if (connection != null && connection.TickCount != 0)
					percentage.GetText = () => "({0}%)".F(orderManager.NetFrameNumber * 100 / connection.TickCount);
				else if (timer != null)
					timer.Bounds.Width += percentage.Bounds.Width;
			}
		}
	}
}
