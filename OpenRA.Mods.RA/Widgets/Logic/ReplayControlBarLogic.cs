#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ReplayControlBarLogic
	{
		enum PlaybackSpeed { Regular, Slow, Fast, Maximum }

		readonly Dictionary<PlaybackSpeed, int> timesteps = new Dictionary<PlaybackSpeed, int>()
		{
			{ PlaybackSpeed.Regular, Game.Timestep },
			{ PlaybackSpeed.Slow, Game.Timestep * 2 },
			{ PlaybackSpeed.Fast, Game.Timestep / 2 },
			{ PlaybackSpeed.Maximum, 1 },
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

				var pauseButton = widget.Get<ButtonWidget>("BUTTON_PAUSE");
				pauseButton.IsVisible = () => world.Timestep != 0 && orderManager.NetFrameNumber < replayNetTicks;
				pauseButton.OnClick = () => world.Timestep = 0;

				var playButton = widget.Get<ButtonWidget>("BUTTON_PLAY");
				playButton.IsVisible = () => world.Timestep == 0 || orderManager.NetFrameNumber >= replayNetTicks;
				playButton.OnClick = () => world.Timestep = timesteps[speed];
				playButton.IsDisabled = () => orderManager.NetFrameNumber >= replayNetTicks;

				var slowButton = widget.Get<ButtonWidget>("BUTTON_SLOW");
				slowButton.IsHighlighted = () => speed == PlaybackSpeed.Slow;
				slowButton.IsDisabled = () => orderManager.NetFrameNumber >= replayNetTicks;
				slowButton.OnClick = () =>
				{
					speed = PlaybackSpeed.Slow;
					if (world.Timestep != 0)
						world.Timestep = timesteps[speed];
				};

				var normalSpeedButton = widget.Get<ButtonWidget>("BUTTON_REGULAR");
				normalSpeedButton.IsHighlighted = () => speed == PlaybackSpeed.Regular;
				normalSpeedButton.IsDisabled = () => orderManager.NetFrameNumber >= replayNetTicks;
				normalSpeedButton.OnClick = () =>
				{
					speed = PlaybackSpeed.Regular;
					if (world.Timestep != 0)
						world.Timestep = timesteps[speed];
				};

				var fastButton = widget.Get<ButtonWidget>("BUTTON_FAST");
				fastButton.IsHighlighted = () => speed == PlaybackSpeed.Fast;
				fastButton.IsDisabled = () => orderManager.NetFrameNumber >= replayNetTicks;
				fastButton.OnClick = () =>
				{
					speed = PlaybackSpeed.Fast;
					if (world.Timestep != 0)
						world.Timestep = timesteps[speed];
				};

				var maximumButton = widget.Get<ButtonWidget>("BUTTON_MAXIMUM");
				maximumButton.IsHighlighted = () => speed == PlaybackSpeed.Maximum;
				maximumButton.IsDisabled = () => orderManager.NetFrameNumber >= replayNetTicks;
				maximumButton.OnClick = () =>
				{
					speed = PlaybackSpeed.Maximum;
					if (world.Timestep != 0)
						world.Timestep = timesteps[speed];
				};
			}
		}
	}
}
