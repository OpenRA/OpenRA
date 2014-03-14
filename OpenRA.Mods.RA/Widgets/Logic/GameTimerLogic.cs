#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class GameTimerLogic
	{
		[ObjectCreator.UseCtor]
		public GameTimerLogic(Widget widget, OrderManager orderManager, World world)
		{
			var timer = widget.GetOrNull<LabelWidget>("GAME_TIMER");
			if (timer != null)
				timer.GetText = () => WidgetUtils.FormatTime(world.WorldTick);

			var status = widget.GetOrNull<LabelWidget>("GAME_TIMER_STATUS");
			if (status != null)
			{
				var startTick = Ui.LastTickTime;
				// Blink the status line
				status.IsVisible = () => (world.Paused || world.Timestep != Game.Timestep)
					&& (Ui.LastTickTime - startTick) / 1000 % 2 == 0;

				status.GetText = () =>
				{
					if (world.Paused || world.Timestep == 0)
						return "Paused";

					if (world.Timestep == 1)
						return "Max Speed";

					return "{0:F1}x Speed".F(Game.Timestep * 1f / world.Timestep);
				};
			}
		}
	}
}
