#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LoadPlayerLogic
	{
		[ObjectCreator.UseCtor]
		public LoadPlayerLogic(Widget widget, World world)
		{
			var ingameRoot = widget.Get("INGAME_ROOT");
			var worldRoot = ingameRoot.Get("WORLD_ROOT");
			var playerRoot = worldRoot.Get("PLAYER_ROOT");

			if (world.Type == WorldType.Regular && world.LocalPlayer != null)
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
		}
	}
}
