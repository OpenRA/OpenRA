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
		[TranslationReference]
		const string Unmute = "label-unmute-player";

		[TranslationReference]
		const string Mute = "label-mute-player";

		[TranslationReference]
		const string Accomplished = "label-mission-accomplished";

		[TranslationReference]
		const string Failed = "label-mission-failed";

		[TranslationReference]
		const string InProgress = "label-mission-in-progress";

		[TranslationReference("team")]
		const string TeamNumber = "label-team-name";

		[TranslationReference]
		const string NoTeam = "label-no-team";

		[TranslationReference]
		const string Spectators = "label-spectators";

		[TranslationReference]
		const string Gone = "label-client-state-disconnected";

		[TranslationReference]
		const string KickTooltip = "button-kick-player";

		[TranslationReference("player")]
		const string KickTitle = "dialog-kick.title";

		[TranslationReference]
		const string KickPrompt = "dialog-kick.prompt";

		[TranslationReference]
		const string KickAccept = "dialog-kick.confirm";

		[TranslationReference]
		const string KickVoteTooltip = "button-vote-kick-player";

		[TranslationReference("player")]
		const string VoteKickTitle = "dialog-vote-kick.title";

		[TranslationReference]
		const string VoteKickPrompt = "dialog-vote-kick.prompt";

		[TranslationReference("bots")]
		const string VoteKickPromptBreakBots = "dialog-vote-kick.prompt-break-bots";

		[TranslationReference]
		const string VoteKickVoteStart = "dialog-vote-kick.vote-start";

		[TranslationReference]
		const string VoteKickVoteFor = "dialog-vote-kick.vote-for";

		[TranslationReference]
		const string VoteKickVoteAgainst = "dialog-vote-kick.vote-against";

		[TranslationReference]
		const string VoteKickVoteCancel = "dialog-vote-kick.vote-cancel";

		[ObjectCreator.UseCtor]
		public GameInfoStatsLogic(Widget widget, ModData modData, World world, OrderManager orderManager, WorldRenderer worldRenderer, Action<bool> hideMenu, Action closeMenu)
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

				var failed = TranslationProvider.GetString(Failed);
				var inProgress = TranslationProvider.GetString(InProgress);
				var accomplished = TranslationProvider.GetString(Accomplished);
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
			var unmuteTooltip = TranslationProvider.GetString(Unmute);
			var muteTooltip = TranslationProvider.GetString(Mute);
			var kickTooltip = TranslationProvider.GetString(KickTooltip);
			var voteKickTooltip = TranslationProvider.GetString(KickVoteTooltip);
			playerPanel.RemoveChildren();

			var teams = world.Players.Where(p => !p.NonCombatant && p.Playable)
				.Select(p => (Player: p, PlayerStatistics: p.PlayerActor.TraitOrDefault<PlayerStatistics>()))
				.OrderByDescending(p => p.PlayerStatistics?.Experience ?? 0)
				.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.Player.ClientIndex) ?? new Session.Client()).Team)
				.OrderByDescending(g => g.Sum(gg => gg.PlayerStatistics?.Experience ?? 0));

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
							titleArguments: Translation.Arguments("player", client.Name),
							text: botsCount > 0 ? VoteKickPromptBreakBots : VoteKickPrompt,
							textArguments: Translation.Arguments("bots", botsCount),
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
						titleArguments: Translation.Arguments("player", client.Name),
						text: botsCount > 0 ? VoteKickPromptBreakBots : VoteKickPrompt,
						textArguments: Translation.Arguments("bots", botsCount),
						onConfirm: () =>
						{
							orderManager.IssueOrder(Order.Command($"vote_kick {client.Index} {true}"));
							hideMenu(false);
							closeMenu();
						},
						confirmText: VoteKickVoteFor,
						onOther: () =>
						{
							Ui.CloseWindow();
							orderManager.IssueOrder(Order.Command($"vote_kick {client.Index} {false}"));
							hideMenu(false);
							closeMenu();
						},
						otherText: VoteKickVoteAgainst,
						onCancel: () => hideMenu(false),
						cancelText: VoteKickVoteCancel);
				}
				else
				{
					ConfirmationDialogs.ButtonPrompt(modData,
						title: KickTitle,
						titleArguments: Translation.Arguments("player", client.Name),
						text: KickPrompt,
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
				if (teams.Count() > 1)
				{
					var teamHeader = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
					var team = t.Key > 0
						? TranslationProvider.GetString(TeamNumber, Translation.Arguments("team", t.Key))
						: TranslationProvider.GetString(NoTeam);
					teamHeader.Get<LabelWidget>("TEAM").GetText = () => team;
					var teamRating = teamHeader.Get<LabelWidget>("TEAM_SCORE");
					var scoreCache = new CachedTransform<int, string>(s => s.ToString());
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
						factionName = pp.Faction.Name != factionName ? $"{factionName} ({pp.Faction.Name})" : pp.Faction.Name;
					}
					else
						flag.GetImageName = () => pp.DisplayFaction.InternalName;

					WidgetUtils.TruncateLabelToTooltip(item.Get<LabelWithTooltipWidget>("FACTION"), factionName);

					var scoreCache = new CachedTransform<int, string>(s => s.ToString());
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
				var spectatorTeam = TranslationProvider.GetString(Spectators);
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
						var suffix = client.State == Session.ClientState.Disconnected ? $" ({TranslationProvider.GetString(Gone)})" : "";
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
