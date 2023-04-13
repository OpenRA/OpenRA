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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("OpenTeamChat", "OpenGeneralChat")]
	public class IngameChatLogic : ChromeLogic, INotificationHandler<TextNotification>
	{
		[TranslationReference]
		const string TeamChat = "button-team-chat";

		[TranslationReference]
		const string GeneralChat = "button-general-chat";

		[TranslationReference("seconds")]
		const string ChatAvailability = "label-chat-availability";

		[TranslationReference]
		const string ChatDisabled = "label-chat-disabled";

		readonly Ruleset modRules;
		readonly World world;

		readonly ContainerWidget chatOverlay;
		readonly TextNotificationsDisplayWidget chatOverlayDisplay;

		readonly ContainerWidget chatChrome;
		readonly ScrollPanelWidget chatScrollPanel;
		readonly TextFieldWidget chatText;
		readonly CachedTransform<int, string> chatAvailableIn;
		readonly string chatDisabled;
		readonly Dictionary<TextNotificationPool, Widget> templates = new();

		readonly TabCompletionLogic tabCompletion = new();

		readonly string chatLineSound = ChromeMetrics.Get<string>("ChatLineSound");

		bool chatEnabled;

		readonly bool isMenuChat;

		[ObjectCreator.UseCtor]
		public IngameChatLogic(Widget widget, OrderManager orderManager, World world, ModData modData, bool isMenuChat, Dictionary<string, MiniYaml> logicArgs)
		{
			modRules = modData.DefaultRules;
			this.isMenuChat = isMenuChat;
			this.world = world;

			var chatTraits = world.WorldActor.TraitsImplementing<INotifyChat>().ToArray();
			var players = world.Players.Where(p => p != world.LocalPlayer && !p.NonCombatant && !p.IsBot);
			var isObserver = orderManager.LocalClient != null && orderManager.LocalClient.IsObserver;
			var alwaysDisabled = world.IsReplay || world.LobbyInfo.NonBotClients.Count() == 1;
			var disableTeamChat = alwaysDisabled || (world.LocalPlayer != null && !players.Any(p => p.IsAlliedWith(world.LocalPlayer)));
			var teamChat = !disableTeamChat;

			var teamMessage = TranslationProvider.GetString(TeamChat);
			var allMessage = TranslationProvider.GetString(GeneralChat);

			chatDisabled = TranslationProvider.GetString(ChatDisabled);

			// Only execute this once, the first time this widget is loaded
			if (TextNotificationsManager.MutedPlayers.Count == 0)
				foreach (var c in orderManager.LobbyInfo.Clients)
					TextNotificationsManager.MutedPlayers.Add(c.Index, false);

			tabCompletion.Commands = chatTraits.OfType<ChatCommands>().ToArray().SelectMany(x => x.Commands.Keys);
			tabCompletion.Names = orderManager.LobbyInfo.Clients.Select(c => c.Name).Distinct().ToList();

			if (logicArgs.TryGetValue("Templates", out var templateIds))
			{
				foreach (var item in templateIds.Nodes)
				{
					var key = FieldLoader.GetValue<TextNotificationPool>("key", item.Key);
					templates[key] = Ui.LoadWidget(item.Value.Value, null, new WidgetArgs());
				}
			}

			var chatPanel = (ContainerWidget)widget;
			chatOverlay = chatPanel.GetOrNull<ContainerWidget>("CHAT_OVERLAY");
			if (chatOverlay != null)
			{
				chatOverlayDisplay = chatOverlay.Get<TextNotificationsDisplayWidget>("CHAT_DISPLAY");
				chatOverlay.Visible = false;
			}

			chatChrome = chatPanel.Get<ContainerWidget>("CHAT_CHROME");
			chatChrome.Visible = true;

			var chatMode = chatChrome.Get<ButtonWidget>("CHAT_MODE");
			chatMode.GetText = () => teamChat && !disableTeamChat ? teamMessage : allMessage;
			chatMode.OnClick = () => teamChat ^= true;

			// Enable teamchat if we are a player and die,
			// or disable it when we are the only one left in the team
			if (!alwaysDisabled && world.LocalPlayer != null)
			{
				chatMode.IsDisabled = () =>
				{
					if (world.IsGameOver || !chatEnabled)
						return true;

					// The game is over for us, join spectator team chat
					if (world.LocalPlayer.WinState != WinState.Undefined)
					{
						disableTeamChat = false;
						return disableTeamChat;
					}

					// If team chat isn't already disabled, check if we are the only living team member
					if (!disableTeamChat)
						disableTeamChat = players.All(p => p.WinState != WinState.Undefined || !p.IsAlliedWith(world.LocalPlayer));

					return disableTeamChat;
				};
			}
			else
				chatMode.IsDisabled = () => disableTeamChat || !chatEnabled;

			// Disable team chat after the game ended
			world.GameOver += () => disableTeamChat = true;

			chatText = chatChrome.Get<TextFieldWidget>("CHAT_TEXTFIELD");
			chatText.MaxLength = UnitOrders.ChatMessageMaxLength;
			chatText.OnEnterKey = _ =>
			{
				var team = teamChat && !disableTeamChat;
				if (chatText.Text != "")
				{
					if (!chatText.Text.StartsWith("/", StringComparison.Ordinal))
					{
						// This should never happen, but avoid a crash if it does somehow (chat will just stay open)
						if (!isObserver && orderManager.LocalClient == null && world.LocalPlayer == null)
							return true;

						var teamNumber = 0U;
						if (team)
							teamNumber = (isObserver || world.LocalPlayer.WinState != WinState.Undefined) ? uint.MaxValue : (uint)orderManager.LocalClient.Team;

						orderManager.IssueOrder(Order.Chat(chatText.Text.Trim(), teamNumber));
					}
					else if (chatTraits != null)
					{
						var text = chatText.Text.Trim();
						var from = world.IsReplay ? null : orderManager.LocalClient.Name;
						foreach (var trait in chatTraits)
							trait.OnChat(from, text);
					}
				}

				chatText.Text = "";
				if (!isMenuChat)
					CloseChat();

				return true;
			};

			chatText.OnTabKey = e =>
			{
				if (!chatMode.Key.IsActivatedBy(e) || chatMode.IsDisabled())
				{
					chatText.Text = tabCompletion.Complete(chatText.Text);
					chatText.CursorPosition = chatText.Text.Length;
				}
				else
					chatMode.OnKeyPress(e);

				return true;
			};

			chatText.OnEscKey = _ =>
			{
				if (!isMenuChat)
					CloseChat();
				else
					chatText.YieldKeyboardFocus();

				return true;
			};

			chatAvailableIn = new CachedTransform<int, string>(x => TranslationProvider.GetString(ChatAvailability, Translation.Arguments("seconds", x)));

			if (!isMenuChat)
			{
				var openTeamChatKey = new HotkeyReference();
				if (logicArgs.TryGetValue("OpenTeamChatKey", out var hotkeyArg))
					openTeamChatKey = modData.Hotkeys[hotkeyArg.Value];

				var openGeneralChatKey = new HotkeyReference();
				if (logicArgs.TryGetValue("OpenGeneralChatKey", out hotkeyArg))
					openGeneralChatKey = modData.Hotkeys[hotkeyArg.Value];

				var chatClose = chatChrome.Get<ButtonWidget>("CHAT_CLOSE");
				chatClose.OnClick += CloseChat;

				chatPanel.OnKeyPress = e =>
				{
					if (e.Event == KeyInputEvent.Up)
						return false;

					if (!chatChrome.IsVisible() && (openTeamChatKey.IsActivatedBy(e) || openGeneralChatKey.IsActivatedBy(e)))
					{
						teamChat = !disableTeamChat && !openGeneralChatKey.IsActivatedBy(e);

						OpenChat();
						return true;
					}

					return false;
				};
			}

			chatScrollPanel = chatChrome.Get<ScrollPanelWidget>("CHAT_SCROLLPANEL");
			chatScrollPanel.RemoveChildren();
			chatScrollPanel.ScrollToBottom();

			foreach (var notification in TextNotificationsManager.Notifications)
				if (IsNotificationEligible(notification))
					AddNotification(notification, true);

			chatText.IsDisabled = () => !chatEnabled || (world.IsReplay && !Game.Settings.Debug.EnableDebugCommandsInReplays);

			if (!isMenuChat)
			{
				CloseChat();

				var keyListener = chatChrome.Get<LogicKeyListenerWidget>("KEY_LISTENER");
				keyListener.AddHandler(e =>
				{
					if (e.Event == KeyInputEvent.Up || !chatText.IsDisabled())
						return false;

					if ((e.Key == Keycode.RETURN || e.Key == Keycode.KP_ENTER || e.Key == Keycode.ESCAPE) && e.Modifiers == Modifiers.None)
					{
						CloseChat();
						return true;
					}

					return false;
				});
			}

			if (logicArgs.TryGetValue("ChatLineSound", out var yaml))
				chatLineSound = yaml.Value;
		}

		public void OpenChat()
		{
			chatText.Text = "";
			chatChrome.Visible = true;
			chatScrollPanel.ScrollToBottom();
			if (!chatText.IsDisabled())
				chatText.TakeKeyboardFocus();

			chatOverlay.Visible = false;
		}

		public void CloseChat()
		{
			chatChrome.Visible = false;
			chatText.YieldKeyboardFocus();
			chatOverlay.Visible = true;
			Ui.ResetTooltips();
		}

		void INotificationHandler<TextNotification>.Handle(TextNotification notification)
		{
			if (!IsNotificationEligible(notification))
				return;

			if (notification.ClientId != TextNotificationsManager.SystemClientId && TextNotificationsManager.MutedPlayers[notification.ClientId])
				return;

			if (!IsNotificationMuted(notification))
				chatOverlayDisplay?.AddNotification(notification);

			// HACK: Force disable the chat notification sound for the in-menu chat dialog
			// This works around our inability to disable the sounds for the in-game dialog when it is hidden
			AddNotification(notification, chatOverlay == null);
		}

		void AddNotification(TextNotification notification, bool suppressSound)
		{
			var chatLine = templates[notification.Pool].Clone();
			WidgetUtils.SetupTextNotification(chatLine, notification, chatScrollPanel.Bounds.Width - chatScrollPanel.ScrollbarWidth, isMenuChat && !world.IsReplay);

			var scrolledToBottom = chatScrollPanel.ScrolledToBottom;
			chatScrollPanel.AddChild(chatLine);
			if (scrolledToBottom)
				chatScrollPanel.ScrollToBottom(smooth: true);

			if (!suppressSound && !IsNotificationMuted(notification))
				Game.Sound.PlayNotification(modRules, null, "Sounds", chatLineSound, null);
		}

		public override void Tick()
		{
			var chatWasEnabled = chatEnabled;
			chatEnabled = world.IsReplay || (Game.RunTime >= TextNotificationsManager.ChatDisabledUntil && TextNotificationsManager.ChatDisabledUntil != uint.MaxValue);

			if (chatEnabled && !chatWasEnabled)
			{
				chatText.Text = "";
				if (Ui.KeyboardFocusWidget == null && chatChrome.Visible)
					chatText.TakeKeyboardFocus();
			}
			else if (!chatEnabled)
			{
				var remaining = 0;
				if (TextNotificationsManager.ChatDisabledUntil != uint.MaxValue)
					remaining = (int)(TextNotificationsManager.ChatDisabledUntil - Game.RunTime + 999) / 1000;

				chatText.Text = remaining == 0 ? chatDisabled : chatAvailableIn.Update(remaining);
			}
		}

		static bool IsNotificationEligible(TextNotification notification)
		{
			return notification.Pool == TextNotificationPool.Chat ||
				notification.Pool == TextNotificationPool.System ||
				notification.Pool == TextNotificationPool.Mission;
		}

		bool IsNotificationMuted(TextNotification notification)
		{
			return Game.Settings.Game.HideReplayChat && world.IsReplay && notification.ClientId != TextNotificationsManager.SystemClientId;
		}
	}
}
