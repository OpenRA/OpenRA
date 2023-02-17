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
using System.Collections.Generic;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ReplayControlBarLogic : ChromeLogic
	{
		enum PlaybackSpeed { Regular, Slow, Fast, Maximum }

		readonly Dictionary<PlaybackSpeed, float> multipliers = new Dictionary<PlaybackSpeed, float>()
		{
			{ PlaybackSpeed.Regular, 1 },
			{ PlaybackSpeed.Slow, 2 },
			{ PlaybackSpeed.Fast, 0.5f },
			{ PlaybackSpeed.Maximum, 0.001f },
		};

		[ObjectCreator.UseCtor]
		public ReplayControlBarLogic(Widget widget, World world, OrderManager orderManager)
		{
			if (world.IsReplay)
			{
				var container = widget.Get("REPLAY_PLAYER");
				var connection = (ReplayConnection)orderManager.Connection;
				var replayNetTicks = connection.TickCount;

				var background = widget.Parent.GetOrNull("OBSERVER_CONTROL_BG");
				if (background != null)
					background.Bounds.Height += container.Bounds.Height;

				container.Visible = true;
				var speed = PlaybackSpeed.Regular;
				var originalTimestep = world.Timestep;

				// In the event the replay goes out of sync, it becomes no longer usable. For polish we permanently pause the world.
				Func<bool> isWidgetDisabled = () => orderManager.IsOutOfSync || orderManager.NetFrameNumber >= replayNetTicks;

				var pauseButton = widget.Get<ButtonWidget>("BUTTON_PAUSE");
				pauseButton.IsVisible = () => world.ReplayTimestep != 0 && !isWidgetDisabled();
				pauseButton.OnClick = () => world.ReplayTimestep = 0;

				var playButton = widget.Get<ButtonWidget>("BUTTON_PLAY");
				playButton.IsVisible = () => world.ReplayTimestep == 0 || isWidgetDisabled();
				playButton.OnClick = () => world.ReplayTimestep = (int)Math.Ceiling(originalTimestep * multipliers[speed]);
				playButton.IsDisabled = isWidgetDisabled;

				var slowButton = widget.Get<ButtonWidget>("BUTTON_SLOW");
				slowButton.IsHighlighted = () => speed == PlaybackSpeed.Slow;
				slowButton.IsDisabled = isWidgetDisabled;
				slowButton.OnClick = () =>
				{
					speed = PlaybackSpeed.Slow;
					if (world.ReplayTimestep != 0)
						world.ReplayTimestep = (int)Math.Ceiling(originalTimestep * multipliers[speed]);
				};

				var normalSpeedButton = widget.Get<ButtonWidget>("BUTTON_REGULAR");
				normalSpeedButton.IsHighlighted = () => speed == PlaybackSpeed.Regular;
				normalSpeedButton.IsDisabled = isWidgetDisabled;
				normalSpeedButton.OnClick = () =>
				{
					speed = PlaybackSpeed.Regular;
					if (world.ReplayTimestep != 0)
						world.ReplayTimestep = (int)Math.Ceiling(originalTimestep * multipliers[speed]);
				};

				var fastButton = widget.Get<ButtonWidget>("BUTTON_FAST");
				fastButton.IsHighlighted = () => speed == PlaybackSpeed.Fast;
				fastButton.IsDisabled = isWidgetDisabled;
				fastButton.OnClick = () =>
				{
					speed = PlaybackSpeed.Fast;
					if (world.ReplayTimestep != 0)
						world.ReplayTimestep = (int)Math.Ceiling(originalTimestep * multipliers[speed]);
				};

				var maximumButton = widget.Get<ButtonWidget>("BUTTON_MAXIMUM");
				maximumButton.IsHighlighted = () => speed == PlaybackSpeed.Maximum;
				maximumButton.IsDisabled = isWidgetDisabled;
				maximumButton.OnClick = () =>
				{
					speed = PlaybackSpeed.Maximum;
					if (world.ReplayTimestep != 0)
						world.ReplayTimestep = (int)Math.Ceiling(originalTimestep * multipliers[speed]);
				};
			}
		}
	}
}
