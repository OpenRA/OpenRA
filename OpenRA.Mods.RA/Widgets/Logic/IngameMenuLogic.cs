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
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	class IngameMenuLogic
	{
		[ObjectCreator.UseCtor]
		public IngameMenuLogic(Widget widget, World world, Action onExit, WorldRenderer worldRenderer, bool transient)
		{
			var resumeDisabled = false;
			var mpe = world.WorldActor.TraitOrDefault<MenuPaletteEffect>();

			Action onQuit = () =>
			{
				Sound.PlayNotification(world.Map.Rules, null, "Speech", "Leave", world.LocalPlayer == null ? null : world.LocalPlayer.Country.Race);
				resumeDisabled = true;

				var exitDelay = 1200;
				if (mpe != null)
				{
					Game.RunAfterDelay(exitDelay, () => mpe.Fade(MenuPaletteEffect.EffectType.Black));
					exitDelay += 40 * mpe.Info.FadeLength;
				}

				Game.RunAfterDelay(exitDelay, () =>
				{
					Game.Disconnect();
					Ui.ResetAll();
					Game.LoadShellMap();
				});
			};

			Action onSurrender = () =>
			{
				world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
				onExit();
			};

			widget.Get<ButtonWidget>("DISCONNECT").OnClick = () =>
			{
				widget.Visible = false;
				ConfirmationDialogs.PromptConfirmAction("Abort Mission", "Leave this game and return to the menu?", onQuit, () => widget.Visible = true);
			};

			widget.Get<ButtonWidget>("SETTINGS").OnClick = () =>
			{
				widget.Visible = false;
				Ui.OpenWindow("SETTINGS_PANEL", new WidgetArgs()
				{
					{ "onExit", () => widget.Visible = true },
					{ "worldRenderer", worldRenderer },
				});
			};

			widget.Get<ButtonWidget>("MUSIC").OnClick = () =>
			{
				widget.Visible = false;
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs
				{
					{ "onExit", () => { widget.Visible = true; } },
					{ "world", world }
				});
			};

			var resumeButton = widget.Get<ButtonWidget>("RESUME");
			resumeButton.IsDisabled = () => resumeDisabled;
			resumeButton.OnClick = () =>
			{
				if (transient)
				{
					Ui.CloseWindow();
					Ui.Root.RemoveChild(widget);
				}

				onExit();
			};

			widget.Get<ButtonWidget>("SURRENDER").OnClick = () =>
			{
				widget.Visible = false;
				ConfirmationDialogs.PromptConfirmAction(
					"Surrender",
					"Are you sure you want to surrender?",
					onSurrender,
					() => widget.Visible = true,
					"Surrender");
			};
			widget.Get("SURRENDER").IsVisible = () => world.LocalPlayer != null && world.LocalPlayer.WinState == WinState.Undefined;
		}
	}
}
