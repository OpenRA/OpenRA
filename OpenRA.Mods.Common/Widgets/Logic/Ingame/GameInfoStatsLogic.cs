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
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	sealed class GameInfoStatsLogic : ChromeLogic
	{
		[FluentReference]
		const string Unmute = "label-unmute-player";

		[FluentReference]
		const string Mute = "label-mute-player";

		[FluentReference]
		const string Accomplished = "label-mission-accomplished";

		[FluentReference]
		const string Failed = "label-mission-failed";

		[FluentReference]
		const string InProgress = "label-mission-in-progress";

		[FluentReference("team")]
		const string TeamNumber = "label-team-name";

		[FluentReference]
		const string NoTeam = "label-no-team";

		[FluentReference]
		const string Spectators = "label-spectators";

		[FluentReference]
		const string Gone = "label-client-state-disconnected";

		[FluentReference]
		const string KickTooltip = "button-kick-player";

		[FluentReference("player")]
		const string KickTitle = "dialog-kick.title";

		[FluentReference]
		const string KickPrompt = "dialog-kick.prompt";

		[FluentReference]
		const string KickAccept = "dialog-kick.confirm";

		[FluentReference]
		const string KickVoteTooltip = "button-vote-kick-player";

		[FluentReference("player")]
		const string VoteKickTitle = "dialog-vote-kick.title";

		[FluentReference]
		const string VoteKickPrompt = "dialog-vote-kick.prompt";

		[FluentReference("bots")]
		const string VoteKickPromptBreakBots = "dialog-vote-kick.prompt-break-bots";

		[FluentReference]
		const string VoteKickVoteStart = "dialog-vote-kick.vote-start";

		[FluentReference]
		const string VoteKickVoteFor = "dialog-vote-kick.vote-for";

		[FluentReference]
		const string VoteKickVoteAgainst = "dialog-vote-kick.vote-against";

		[FluentReference]
		const string VoteKickVoteCancel = "dialog-vote-kick.vote-cancel";

		[ObjectCreator.UseCtor]
		public GameInfoStatsLogic(Widget widget, ModData modData, World world,
			OrderManager orderManager, WorldRenderer worldRenderer, Action<bool> hideMenu, Action closeMenu)
		{
			var player = world.LocalPlayer;
			var playerPanel = widget.Get<ScrollPanelWidget>("PLAYER_LIST");
			var statsHeader = widget.Get("STATS_HEADERS");

			if (player != null && !player.NonCombatant)
			{
				var checkbox = widget.Get<CheckboxWidget>("STATS_CHECKBOX");
				var statusLabel = widget.Get<LabelWidget>("STATS_STATUS");

				checkbox.IsChecked = () => player.WinState != WinState.Undefined;
				checkbox.GetCheckmark = () => player.WinState == WinState.Won ? "tick" : "cross";

				if (player.HasObjectives)
				{
					var mo = player.PlayerActor.Trait<MissionObjectives>();
					checkbox.GetText = () => mo.Objectives[0].Description;
				}

				var failed = FluentProvider.GetMessage(Failed);
				var inProgress = FluentProvider.GetMessage(InProgress);
				var accomplished = FluentProvider.GetMessage(Accomplished);
				statusLabel.GetText = () => player.WinState == WinState.Won ? accomplished :
					player.WinState == WinState.Lost ? failed : inProgress;
				statusLabel.GetColor = () => player.WinState == WinState.Won ? Color.LimeGreen :
					player.WinState == WinState.Lost ? Color.Red : Color.White;
			}
			else
			{
				// Expand the stats window to cover the hidden objectives
				var objectiveGroup = widget.Get("OBJECTIVE");

				objectiveGroup.Visible = false;
				statsHeader.Bounds.Y -= objectiveGroup.Bounds.Height;
				playerPanel.Bounds.Y -= objectiveGroup.Bounds.Height;
				playerPanel.Bounds.Height += objectiveGroup.Bounds.Height;
			}

			if (!orderManager.LobbyInfo.Clients.Any(c => !c.IsBot && c.Index != orderManager.LocalClient?.Index && c.State != Session.ClientState.Disconnected))
				statsHeader.Get<LabelWidget>("ACTIONS").Visible = false;

			var teamTemplate = playerPanel.Get<ScrollItemWidget>("TEAM_TEMPLATE");
			var playerTemplate = playerPanel.Get("PLAYER_TEMPLATE");
			var spectatorTemplate = playerPanel.Get("SPECTATOR_TEMPLATE");
			var unmuteTooltip = FluentProvider.GetMessage(Unmute);
			var muteTooltip = FluentProvider.GetMessage(Mute);
			var kickTooltip = FluentProvider.GetMessage(KickTooltip);
			var voteKickTooltip = FluentProvider.GetMessage(KickVoteTooltip);
			playerPanel.RemoveChildren();

			var teams = world.Players.Where(p => !p.NonCombatant && p.Playable)
				.Select(p => (Player: p, PlayerStatistics: p.PlayerActor.TraitOrDefault<PlayerStatistics>()))
				.OrderByDescending(p => p.PlayerStatistics?.Experience ?? 0)
				.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.Player.ClientIndex) ?? new Session.Client()).Team)
				.OrderByDescending(g => g.Sum(gg => gg.PlayerStatistics?.Experience ?? 0))
				.ToList();

			void KickAction(Session.Client client, Func<bool> isVoteKick)
			{
				hideMenu(true);
				if (isVoteKick())
				{
					var botsCount = 0;
					if (client.IsAdmin)
						botsCount = world.Players.Count(p => p.IsBot && p.WinState == WinState.Undefined);

					if (UnitOrders.KickVoteTarget == null)
					{
						ConfirmationDialogs.ButtonPrompt(modData,
							title: VoteKickTitle,
							text: botsCount > 0 ? VoteKickPromptBreakBots : VoteKickPrompt,
							titleArguments: ["player", client.Name],
							textArguments: ["bots", botsCount],
							onConfirm: () =>
							{
								orderManager.IssueOrder(Order.Command($"vote_kick {client.Index} {true}"));
								hideMenu(false);
								closeMenu();
							},
							confirmText: VoteKickVoteStart,
							onCancel: () => hideMenu(false));
						return;
					}

					ConfirmationDialogs.ButtonPrompt(modData,
						title: VoteKickTitle,
						text: botsCount > 0 ? VoteKickPromptBreakBots : VoteKickPrompt,
						titleArguments: ["player", client.Name],
						textArguments: ["bots", botsCount],
						onConfirm: () =>
						{
							orderManager.IssueOrder(Order.Command($"vote_kick {client.Index} {true}"));
							hideMenu(false);
							closeMenu();
						},
						confirmText: VoteKickVoteFor,
						onCancel: () => hideMenu(false),
						cancelText: VoteKickVoteCancel,
						onOther: () =>
						{
							Ui.CloseWindow();
							orderManager.IssueOrder(Order.Command($"vote_kick {client.Index} {false}"));
							hideMenu(false);
							closeMenu();
						},
						otherText: VoteKickVoteAgainst);
				}
				else
				{
					ConfirmationDialogs.ButtonPrompt(modData,
						title: KickTitle,
						text: KickPrompt,
						titleArguments: ["player", client.Name],
						onConfirm: () =>
						{
							orderManager.IssueOrder(Order.Command($"kick {client.Index} {false}"));
							hideMenu(false);
						},
						confirmText: KickAccept,
						onCancel: () => hideMenu(false));
				}
			}

			var localClient = orderManager.LocalClient;
			var localPlayer = localClient == null ? null : world.Players.FirstOrDefault(player => player.ClientIndex == localClient.Index);
			bool LocalPlayerCanKick() => localClient != null
				&& (Game.IsHost || ((!orderManager.LocalClient.IsObserver) && localPlayer.WinState == WinState.Undefined));
			bool CanClientBeKicked(Session.Client client, Func<bool> isVoteKick) =>
				client.Index != localClient.Index && client.State != Session.ClientState.Disconnected
				&& (!client.IsAdmin || orderManager.LobbyInfo.GlobalSettings.Dedicated)
				&& (!isVoteKick() || UnitOrders.KickVoteTarget == null || UnitOrders.KickVoteTarget == client.Index);

			foreach (var t in teams)
			{
				if (teams.Count > 1)
				{
					var teamHeader = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
					var team = t.Key > 0
						? FluentProvider.GetMessage(TeamNumber, "team", t.Key)
						: FluentProvider.GetMessage(NoTeam);
					teamHeader.Get<LabelWidget>("TEAM").GetText = () => team;
					var teamRating = teamHeader.Get<LabelWidget>("TEAM_SCORE");
					var scoreCache = new CachedTransform<int, string>(s => s.ToString(NumberFormatInfo.CurrentInfo));
					var teamMemberScores = t.Select(tt => tt.PlayerStatistics).Where(s => s != null).ToArray().Select(s => s.Experience);
					teamRating.GetText = () => scoreCache.Update(teamMemberScores.Sum());

					playerPanel.AddChild(teamHeader);
				}

				foreach (var p in t.ToList())
				{
					var pp = p.Player;
					var client = world.LobbyInfo.ClientWithIndex(pp.ClientIndex);
					var item = playerTemplate.Clone();
					LobbyUtils.SetupProfileWidget(item, client, orderManager, worldRenderer);

					var nameLabel = item.Get<LabelWidget>("NAME");
					WidgetUtils.BindPlayerNameAndStatus(nameLabel, pp);
					nameLabel.GetColor = () => pp.Color;

					var flag = item.Get<ImageWidget>("FACTIONFLAG");
					flag.GetImageCollection = () => "flags";

					var factionName = pp.DisplayFaction.Name;
					if (player == null || player.RelationshipWith(pp) == PlayerRelationship.Ally || player.WinState != WinState.Undefined)
					{
						flag.GetImageName = () => pp.Faction.InternalName;
						factionName = pp.Faction.Name != factionName
							? $"{FluentProvider.GetMessage(factionName)} ({FluentProvider.GetMessage(pp.Faction.Name)})"
							: FluentProvider.GetMessage(pp.Faction.Name);
					}
					else
					{
						flag.GetImageName = () => pp.DisplayFaction.InternalName;
						factionName = FluentProvider.GetMessage(factionName);
					}

					WidgetUtils.TruncateLabelToTooltip(item.Get<LabelWithTooltipWidget>("FACTION"), factionName);

					var scoreCache = new CachedTransform<int, string>(s => s.ToString(NumberFormatInfo.CurrentInfo));
					item.Get<LabelWidget>("SCORE").GetText = () => scoreCache.Update(p.PlayerStatistics?.Experience ?? 0);

					var muteCheckbox = item.Get<CheckboxWidget>("MUTE");
					muteCheckbox.IsChecked = () => TextNotificationsManager.MutedPlayers[pp.ClientIndex];
					muteCheckbox.OnClick = () => TextNotificationsManager.MutedPlayers[pp.ClientIndex] ^= true;
					muteCheckbox.IsVisible = () => !pp.IsBot && client.State != Session.ClientState.Disconnected && pp.ClientIndex != orderManager.LocalClient?.Index;
					muteCheckbox.GetTooltipText = () => muteCheckbox.IsChecked() ? unmuteTooltip : muteTooltip;

					var kickButton = item.Get<ButtonWidget>("KICK");
					bool IsVoteKick() => !Game.IsHost || pp.WinState == WinState.Undefined;
					kickButton.IsVisible = () => !pp.IsBot && LocalPlayerCanKick() && CanClientBeKicked(client, IsVoteKick);
					kickButton.OnClick = () => KickAction(client, IsVoteKick);
					kickButton.GetTooltipText = () => IsVoteKick() ? voteKickTooltip : kickTooltip;

					playerPanel.AddChild(item);
				}
			}

			var spectators = orderManager.LobbyInfo.Clients.Where(c => c.IsObserver).ToList();
			if (spectators.Count > 0)
			{
				var spectatorHeader = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
				var spectatorTeam = FluentProvider.GetMessage(Spectators);
				spectatorHeader.Get<LabelWidget>("TEAM").GetText = () => spectatorTeam;

				playerPanel.AddChild(spectatorHeader);

				foreach (var client in spectators)
				{
					var item = spectatorTemplate.Clone();
					LobbyUtils.SetupProfileWidget(item, client, orderManager, worldRenderer);

					var nameLabel = item.Get<LabelWidget>("NAME");
					var nameFont = Game.Renderer.Fonts[nameLabel.Font];

					var suffixLength = new CachedTransform<string, int>(s => nameFont.Measure(s).X);
					var name = new CachedTransform<(string Name, string Suffix), string>(c =>
						WidgetUtils.TruncateText(c.Name, nameLabel.Bounds.Width - suffixLength.Update(c.Suffix), nameFont) + c.Suffix);

					nameLabel.GetText = () =>
					{
						var suffix = client.State == Session.ClientState.Disconnected ? $" ({FluentProvider.GetMessage(Gone)})" : "";
						return name.Update((client.Name, suffix));
					};

					var kickButton = item.Get<ButtonWidget>("KICK");
					bool IsVoteKick() => !Game.IsHost;
					kickButton.IsVisible = () => LocalPlayerCanKick() && CanClientBeKicked(client, IsVoteKick);
					kickButton.OnClick = () => KickAction(client, IsVoteKick);
					kickButton.GetTooltipText = () => IsVoteKick() ? voteKickTooltip : kickTooltip;

					var muteCheckbox = item.Get<CheckboxWidget>("MUTE");
					muteCheckbox.IsChecked = () => TextNotificationsManager.MutedPlayers[client.Index];
					muteCheckbox.OnClick = () => TextNotificationsManager.MutedPlayers[client.Index] ^= true;
					muteCheckbox.IsVisible = () => !client.IsBot && client.State != Session.ClientState.Disconnected && client.Index != orderManager.LocalClient?.Index;
					muteCheckbox.GetTooltipText = () => muteCheckbox.IsChecked() ? unmuteTooltip : muteTooltip;

					playerPanel.AddChild(item);
				}
			}
		}
	}
}
