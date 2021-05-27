#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
			var tlm = world.WorldActor.TraitOrDefault<TimeLimitManager>();
			var startTick = Ui.LastTickTime.Value;

			Func<bool> shouldShowStatus = () => (world.Paused || world.ReplayTimestep != world.Timestep)
				&& (Ui.LastTickTime.Value - startTick) / 1000 % 2 == 0;

			Func<string> statusText = () =>
			{
				if (world.Paused || world.ReplayTimestep == 0)
					return "Paused";

				if (world.ReplayTimestep == 1)
					return "Max Speed";

				return $"{world.Timestep * 100 / world.ReplayTimestep}% Speed";
			};

			if (timer != null)
			{
				timer.GetText = () =>
				{
					if (status == null && shouldShowStatus())
						return statusText();

					var timeLimit = tlm?.TimeLimit ?? 0;
					var displayTick = timeLimit > 0 ? timeLimit - world.WorldTick : world.WorldTick;
					return WidgetUtils.FormatTime(Math.Max(0, displayTick), world.Timestep);
				};
			}

			if (status != null)
			{
				// Blink the status line
				status.IsVisible = shouldShowStatus;
				status.GetText = statusText;
			}

			if (timer is LabelWithTooltipWidget timerTooltip)
			{
				var connection = orderManager.Connection as ReplayConnection;
				if (connection != null && connection.FinalGameTick != 0)
					timerTooltip.GetTooltipText = () => $"{world.WorldTick * 100 / connection.FinalGameTick}% complete";
				else if (connection != null && connection.TickCount != 0)
					timerTooltip.GetTooltipText = () => $"{orderManager.NetFrameNumber * 100 / connection.TickCount}% complete";
				else
					timerTooltip.GetTooltipText = null;
			}
		}
	}
}
