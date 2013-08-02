#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	class IngameMenuLogic
	{
		[ObjectCreator.UseCtor]
		public IngameMenuLogic(Widget widget, World world, Action onExit)
		{
			widget.Get<ButtonWidget>("DISCONNECT").OnClick = () =>
			{
				onExit();
				LeaveGame(world);
			};
			widget.Get<ButtonWidget>("SETTINGS").OnClick = () =>
			{
				widget.Visible = false;
				Ui.OpenWindow("SETTINGS_MENU", new WidgetArgs { { "onExit", () => { widget.Visible = true; } } });
			};
			widget.Get<ButtonWidget>("MUSIC").OnClick = () =>
			{
				widget.Visible = false;
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs { { "onExit", () => { widget.Visible = true; } } });
			};
			widget.Get<ButtonWidget>("RESUME").OnClick = () => onExit();

			widget.Get<ButtonWidget>("SURRENDER").OnClick = () =>
			{
				world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
				onExit();
			};
			widget.Get("SURRENDER").IsVisible = () => world.LocalPlayer != null && world.LocalPlayer.WinState == WinState.Undefined;
		}

		void LeaveGame(World world)
		{
			Sound.PlayNotification(null, "Speech", "Leave", world.LocalPlayer == null ? null : world.LocalPlayer.Country.Race);
			Game.Disconnect();
			Ui.CloseWindow();
			Game.LoadShellMap();
		}
	}
}
