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
	public class LoadIngamePlayerOrObserverUILogic
	{
		[ObjectCreator.UseCtor]
		public LoadIngamePlayerOrObserverUILogic(Widget widget, World world)
		{
			var ingameRoot = widget.Get("INGAME_ROOT");
			var playerRoot = ingameRoot.Get("PLAYER_ROOT");

			if (world.LocalPlayer == null)
				Game.LoadWidget(world, "OBSERVER_WIDGETS", playerRoot, new WidgetArgs());
			else
			{
				var playerWidgets = Game.LoadWidget(world, "PLAYER_WIDGETS", playerRoot, new WidgetArgs());
				var sidebarTicker = playerWidgets.Get<LogicTickerWidget>("SIDEBAR_TICKER");

				sidebarTicker.OnTick = () =>
				{
					// Switch to observer mode after win/loss
					if (world.ObserveAfterWinOrLose && world.LocalPlayer.WinState != WinState.Undefined)
						Game.RunAfterTick(() =>
						{
							playerRoot.RemoveChildren();
							Game.LoadWidget(world, "OBSERVER_WIDGETS", playerRoot, new WidgetArgs());
						});

				};
			}

			Game.LoadWidget(world, "CHAT_PANEL", ingameRoot, new WidgetArgs());
		}
	}
}
