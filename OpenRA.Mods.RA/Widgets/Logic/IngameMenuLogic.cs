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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;
using System.IO;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngameMenuLogic
	{
		Widget menu;

		[ObjectCreator.UseCtor]
		public IngameMenuLogic(Widget widget, World world, Action onExit, WorldRenderer worldRenderer, IngameInfoPanel activePanel)
		{
			var resumeDisabled = false;
			menu = widget.Get("INGAME_MENU");
			var mpe = world.WorldActor.TraitOrDefault<MenuPaletteEffect>();
			if (mpe != null)
				mpe.Fade(mpe.Info.MenuEffect);

			menu.Get<LabelWidget>("VERSION_LABEL").Text = Game.modData.Manifest.Mod.Version;

			var hideMenu = false;
			menu.Get("MENU_BUTTONS").IsVisible = () => !hideMenu;

			// TODO: Create a mechanism to do things like this cleaner. Also needed for scripted missions
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

			Action closeMenu = () =>
			{
				Ui.CloseWindow();
				Ui.Root.RemoveChild(menu);
				if (mpe != null)
					mpe.Fade(MenuPaletteEffect.EffectType.None);
				onExit();
			};

			Action showMenu = () => hideMenu = false;

			menu.Get<ButtonWidget>("ABORT_MISSION").OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.PromptConfirmAction("Abort Mission", "Leave this game and return to the menu?", onQuit, showMenu);
			};

			Action onSurrender = () => 
			{
				world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
				closeMenu();
			};
			var surrenderButton = menu.Get<ButtonWidget>("SURRENDER");
			surrenderButton.IsDisabled = () => (world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Undefined);
			surrenderButton.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.PromptConfirmAction("Surrender", "Are you sure you want to surrender?", onSurrender, showMenu);
			};
			surrenderButton.IsDisabled = () => world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Undefined;

			menu.Get<ButtonWidget>("MUSIC").OnClick = () =>
			{
				hideMenu = true;
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs()
				{
					{ "onExit", () => hideMenu = false },
					{ "world", world }
				});
			};

			var settingsButton = widget.Get<ButtonWidget>("SETTINGS");
			settingsButton.OnClick = () =>
			{
				hideMenu = true;
				Ui.OpenWindow("SETTINGS_PANEL", new WidgetArgs()
				{
					{ "world", world },
					{ "worldRenderer", worldRenderer },
					{ "onExit", () => hideMenu = false },
				});
			};

			Action<string, string> Save = (string dir, string name) =>
			{
				world.Save(dir, name);
			};

			var saveButton = menu.Get<ButtonWidget>("SAVE");
			saveButton.IsDisabled = () => !world.CanSave();
			saveButton.OnClick = () =>
			{
				var mod = Game.modData.Manifest.Mod;
				var dir = new[] { Platform.SupportDir, "Saves", mod.Id, mod.Version }.Aggregate(Path.Combine);
				var invalidChars = Path.GetInvalidFileNameChars();

				ConfirmationDialogs.TextInputPrompt(
					"Save",
					"Enter a file name:",
					"",
					onAccept: new Action<string>(filename => Save(dir, filename)),
					onCancel: null,
					acceptText: "Save",
					cancelText: "Cancel",
					inputValidator: filename =>
					{
						if (filename == "")
							return false;

						if (string.IsNullOrWhiteSpace(filename))
							return false;

						if (filename.IndexOfAny(invalidChars) >= 0)
							return false;

						if (File.Exists(Path.Combine(dir, filename)))
							return false;

						return true;
					});
			};

			var resumeButton = menu.Get<ButtonWidget>("RESUME");
			resumeButton.IsDisabled = () => resumeDisabled;
			resumeButton.OnClick = closeMenu;

			// Game info panel
			var gameInfoPanel = Game.LoadWidget(world, "GAME_INFO_PANEL", menu, new WidgetArgs()
			{
				{ "activePanel", activePanel }
			});
			gameInfoPanel.IsVisible = () => !hideMenu;
		}
	}
}
