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
using System.Drawing;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	class LeaveMapLogic
	{
		enum Tab { Objectives, Chat };

		Tab currentTab;
		bool newChatMessage;

		[ObjectCreator.UseCtor]
		public LeaveMapLogic(Widget widget, World world)
		{
			widget.Get<LabelWidget>("VERSION_LABEL").Text = Game.modData.Manifest.Mod.Version;

			var showStats = false;

			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			var isMultiplayer = !world.LobbyInfo.IsSinglePlayer && !world.IsReplay;
			var hasObjectives = iop != null && iop.PanelName != null && world.LocalPlayer != null;
			var showTabs = hasObjectives && isMultiplayer;
			currentTab = hasObjectives ? Tab.Objectives : Tab.Chat;

			var panelName = hasObjectives || isMultiplayer ? "LEAVE_MAP_FULL" : "LEAVE_MAP_SIMPLE";
			var dialog = widget.Get<ContainerWidget>(panelName);
			dialog.IsVisible = () => !showStats;
			widget.IsVisible = () => Ui.CurrentWindow() == null;

			if (hasObjectives || isMultiplayer)
			{
				var titleText = dialog.Get<LabelWidget>("GAME_ENDED_LABEL");
				var titleTextNoTabs = dialog.GetOrNull<LabelWidget>("GAME_ENDED_LABEL_NO_TABS");
				titleText.IsVisible = () => showTabs || (!showTabs && titleTextNoTabs == null);
				if (titleTextNoTabs != null)
					titleTextNoTabs.IsVisible = () => !showTabs;

				var bg = dialog.Get<BackgroundWidget>("LEAVE_MAP_BG");
				var bgNoTabs = dialog.GetOrNull<BackgroundWidget>("LEAVE_MAP_BG_NO_TABS");
				bg.IsVisible = () => showTabs || (!showTabs && bgNoTabs == null);
				if (bgNoTabs != null)
					bgNoTabs.IsVisible = () => !showTabs;

				var objButton = dialog.Get<ButtonWidget>("OBJECTIVES_BUTTON");
				objButton.IsVisible = () => showTabs;
				objButton.OnClick = () => currentTab = Tab.Objectives;
				objButton.IsHighlighted = () => currentTab == Tab.Objectives;

				var chatButton = dialog.Get<ButtonWidget>("CHAT_BUTTON");
				chatButton.IsVisible = () => showTabs;
				chatButton.OnClick = () =>
				{
					currentTab = Tab.Chat;
					newChatMessage = false;
				};
				chatButton.IsHighlighted = () => currentTab == Tab.Chat || (newChatMessage && Game.LocalTick % 50 < 25);

				Game.BeforeGameStart += UnregisterChatNotification;
				Game.AddChatLine += NotifyNewChatMessage;
			}

			var statsButton = dialog.Get<ButtonWidget>("STATS_BUTTON");
			statsButton.OnClick = () =>
			{
				showStats = true;
				Game.LoadWidget(world, "INGAME_OBSERVERSTATS_BG", Ui.Root, new WidgetArgs()
				{
					{ "onExit", () => showStats = false }
				});
			};

			var leaveButton = dialog.Get<ButtonWidget>("LEAVE_BUTTON");
			leaveButton.OnClick = () =>
			{
				leaveButton.Disabled = true;
				var mpe = world.WorldActor.TraitOrDefault<MenuPaletteEffect>();

				Sound.PlayNotification(world.Map.Rules, null, "Speech", "Leave",
					world.LocalPlayer == null ? null : world.LocalPlayer.Country.Race);

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

			if (hasObjectives)
			{
				var objectivesContainer = dialog.Get<ContainerWidget>("OBJECTIVES_PANEL");
				Game.LoadWidget(world, iop.PanelName, objectivesContainer, new WidgetArgs());
				objectivesContainer.IsVisible = () => currentTab == Tab.Objectives;
			}

			if (isMultiplayer)
			{
				var chatContainer = dialog.Get<ContainerWidget>("DIALOG_CHAT_PANEL");
				chatContainer.IsVisible = () => currentTab == Tab.Chat;
			}
		}

		void NotifyNewChatMessage(Color c, string s1, string s2)
		{
			if (currentTab != Tab.Chat)
				newChatMessage = true;
		}

		void UnregisterChatNotification()
		{
			Game.AddChatLine -= NotifyNewChatMessage;
			Game.BeforeGameStart -= UnregisterChatNotification;
		}
	}
}