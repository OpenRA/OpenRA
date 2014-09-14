#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ReplayControlBarLogic
	{

		[ObjectCreator.UseCtor]
		public ReplayControlBarLogic(Widget widget, World world)
		{
			if (world.IsReplay)
			{
				var container = widget.Get("REPLAY_PLAYER");

				var background = widget.Parent.GetOrNull("OBSERVER_CONTROL_BG");
				if (background != null)
					background.Bounds.Height += container.Bounds.Height;

				container.Visible = true;

				var pauseButton = widget.GetOrNull<ButtonWidget>("BUTTON_PAUSE");
				if (pauseButton != null)
				{
					pauseButton.IsHighlighted = () => world.Timestep == 0;
					pauseButton.OnClick = () => world.Timestep = 0;
				}

				var slowButton = widget.GetOrNull<ButtonWidget>("BUTTON_SLOW");
				if (slowButton != null)
				{
					slowButton.IsHighlighted = () => world.Timestep > Game.Timestep;
					slowButton.OnClick = () => world.Timestep = Game.Timestep * 2;
				}

				var normalSpeedButton = widget.GetOrNull<ButtonWidget>("BUTTON_NORMALSPEED");
				if (normalSpeedButton != null)
				{
					normalSpeedButton.IsHighlighted = () => world.Timestep == Game.Timestep;
					normalSpeedButton.OnClick = () => world.Timestep = Game.Timestep;
				}

				var fastforwardButton = widget.Get<ButtonWidget>("BUTTON_FASTFORWARD");
				fastforwardButton.IsHighlighted = () => world.Timestep == 1;
				fastforwardButton.OnClick = () => world.Timestep = 1;
			}
		}
	}
}
