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

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class GameTimerLogic : ChromeLogic
	{
		[TranslationReference]
		const string Paused = "label-paused";

		[TranslationReference]
		const string MaxSpeed = "label-max-speed";

		[TranslationReference("percentage")]
		const string Speed = "label-replay-speed";

		[TranslationReference("percentage")]
		const string Complete = "label-replay-complete";

		[ObjectCreator.UseCtor]
		public GameTimerLogic(Widget widget, ModData modData, OrderManager orderManager, World world)
		{
			var timer = widget.GetOrNull<LabelWidget>("GAME_TIMER");
			var status = widget.GetOrNull<LabelWidget>("GAME_TIMER_STATUS");
			var tlm = world.WorldActor.TraitOrDefault<TimeLimitManager>();
			var startTick = Ui.LastTickTime.Value;

			Func<bool> shouldShowStatus = () => (world.Paused || world.ReplayTimestep != world.Timestep)
				&& (Ui.LastTickTime.Value - startTick) / 1000 % 2 == 0;

			Func<bool> paused = () => world.Paused || world.ReplayTimestep == 0;

			var pausedText = modData.Translation.GetString(Paused);
			var maxSpeedText = modData.Translation.GetString(MaxSpeed);
			var speedText = new CachedTransform<int, string>(p =>
					modData.Translation.GetString(Speed, Translation.Arguments("percentage", p)));

			if (timer != null)
			{
				timer.GetText = () =>
				{
					if (status == null && paused() && shouldShowStatus())
						return pausedText;

					var timeLimit = tlm?.TimeLimit ?? 0;
					var displayTick = timeLimit > 0 ? timeLimit - world.WorldTick : world.WorldTick;
					return WidgetUtils.FormatTime(Math.Max(0, displayTick), world.Timestep);
				};
			}

			if (status != null)
			{
				// Blink the status line
				status.IsVisible = shouldShowStatus;
				status.GetText = () =>
				{
					if (paused())
						return pausedText;

					if (world.ReplayTimestep == 1)
						return maxSpeedText;

					return speedText.Update(world.Timestep * 100 / world.ReplayTimestep);
				};
			}

			var timerText = new CachedTransform<int, string>(p =>
				modData.Translation.GetString(Complete, Translation.Arguments("percentage", p)));
			if (timer is LabelWithTooltipWidget timerTooltip)
			{
				var connection = orderManager.Connection as ReplayConnection;
				if (connection != null && connection.FinalGameTick != 0)
					timerTooltip.GetTooltipText = () => timerText.Update(world.WorldTick * 100 / connection.FinalGameTick);
				else if (connection != null && connection.TickCount != 0)
					timerTooltip.GetTooltipText = () => timerText.Update(orderManager.NetFrameNumber * 100 / connection.TickCount);
				else
					timerTooltip.GetTooltipText = null;
			}
		}
	}
}
