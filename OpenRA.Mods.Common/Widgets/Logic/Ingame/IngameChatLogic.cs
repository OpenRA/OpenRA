#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameChatLogic : ChromeLogic
	{
		readonly OrderManager orderManager;
		readonly Ruleset modRules;

		readonly ContainerWidget chatOverlay;
		readonly ChatDisplayWidget chatOverlayDisplay;

		readonly ContainerWidget chatChrome;
		readonly ScrollPanelWidget chatScrollPanel;
		readonly ContainerWidget chatTemplate;
		readonly TextFieldWidget chatText;

		readonly INotifyChat[] chatTraits;

		readonly TabCompletionLogic tabCompletion = new TabCompletionLogic();

		readonly string chatLineSound = ChromeMetrics.Get<string>("ChatLineSound");

		[ObjectCreator.UseCtor]
		public IngameChatLogic(Widget widget, OrderManager orderManager, World world, ModData modData, bool isMenuChat, Dictionary<string, MiniYaml> logicArgs)
		{
			this.orderManager = orderManager;
			this.modRules = modData.DefaultRules;

			chatTraits = world.WorldActor.TraitsImplementing<INotifyChat>().ToArray();

			var players = world.Players.Where(p => p != world.LocalPlayer && !p.NonCombatant && !p.IsBot);
			var isObserver = orderManager.LocalClient != null && orderManager.LocalClient.IsObserver;
			var isOnlyObserver = isObserver && orderManager.LobbyInfo.Clients.All(c => c == orderManager.LocalClient || !c.IsObserver);
			var observersExist = orderManager.LobbyInfo.Clients.Any(c => c.IsObserver);
			var alwaysDisabled = world.IsReplay || world.LobbyInfo.NonBotClients.Count() == 1;
			var disableTeamChat = alwaysDisabled || isOnlyObserver || (world.LocalPlayer != null && !players.Any(p => p.IsAlliedWith(world.LocalPlayer)));
			var teamChat = !disableTeamChat;

			tabCompletion.Commands = chatTraits.OfType<ChatCommands>().SelectMany(x => x.Commands.Keys).ToList();
			tabCompletion.Names = orderManager.LobbyInfo.Clients.Select(c => c.Name).Distinct().ToList();

			var chatPanel = (ContainerWidget)widget;
			chatOverlay = chatPanel.GetOrNull<ContainerWidget>("CHAT_OVERLAY");
			if (chatOverlay != null)
			{
				chatOverlayDisplay = chatOverlay.Get<ChatDisplayWidget>("CHAT_DISPLAY");
				chatOverlay.Visible = false;
			}

			chatChrome = chatPanel.Get<ContainerWidget>("CHAT_CHROME");
			chatChrome.Visible = true;

			var chatMode = chatChrome.Get<ButtonWidget>("CHAT_MODE");
			chatMode.GetText = () => teamChat && !disableTeamChat ? "Team" : "All";
			chatMode.OnClick = () => teamChat ^= true;

			// Team chat is disabled if we are the only spectator
			// This changes as soon as a defeated player can talk in the spectator chat
			if (!alwaysDisabled && isOnlyObserver)
			{
				chatMode.IsDisabled = () =>
				{
					if (world.IsGameOver)
						return true;

					disableTeamChat = players.All(p => p.WinState == WinState.Undefined);
					return disableTeamChat;
				};
			}
			else if (!alwaysDisabled && world.LocalPlayer != null)
			{
				chatMode.IsDisabled = () =>
				{
					if (world.IsGameOver)
						return true;

					// Check if we are the only living team member
					if (players.All(p => p.WinState != WinState.Undefined || !p.IsAlliedWith(world.LocalPlayer)))
					{
						disableTeamChat = true;
						return disableTeamChat;
					}

					// Still alive and nothing changed since the start
					if (world.LocalPlayer.WinState == WinState.Undefined)
						return disableTeamChat;

					// At this point our player is dead
					// Allow to chat with existing spectators
					if (observersExist)
					{
						disableTeamChat = false;
						return disableTeamChat;
					}

					// Or wait until another player died
					disableTeamChat = players.All(p => p.WinState == WinState.Undefined);
					return disableTeamChat;
				};
			}
			else
				chatMode.IsDisabled = () => disableTeamChat;

			// Disable team chat after the game ended
			world.GameOver += () => disableTeamChat = true;

			chatText = chatChrome.Get<TextFieldWidget>("CHAT_TEXTFIELD");
			chatText.MaxLength = UnitOrders.ChatMessageMaxLength;
			chatText.OnEnterKey = () =>
			{
				var team = teamChat && !disableTeamChat;
				if (chatText.Text != "")
				{
					if (!chatText.Text.StartsWith("/", StringComparison.Ordinal))
					{
						// This should never happen, but avoid a crash if it does somehow (chat will just stay open)
						if (!isObserver && orderManager.LocalClient == null && world.LocalPlayer == null)
							return true;

						var teamNumber = (uint)0;
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

			chatText.OnTabKey = () =>
			{
				var previousText = chatText.Text;
				chatText.Text = tabCompletion.Complete(chatText.Text);
				chatText.CursorPosition = chatText.Text.Length;

				if (chatText.Text == previousText && !disableTeamChat)
					teamChat ^= true;

				return true;
			};

			chatText.OnEscKey = () =>
			{
				if (!isMenuChat)
					CloseChat();
				else
					chatText.YieldKeyboardFocus();

				return true;
			};

			if (!isMenuChat)
			{
				var chatClose = chatChrome.Get<ButtonWidget>("CHAT_CLOSE");
				chatClose.OnClick += CloseChat;

				chatPanel.OnKeyPress = e =>
				{
					if (e.Event == KeyInputEvent.Up)
						return false;

					if (!chatChrome.IsVisible() && (e.Key == Keycode.RETURN || e.Key == Keycode.KP_ENTER))
					{
						OpenChat();
						return true;
					}

					return false;
				};
			}

			chatScrollPanel = chatChrome.Get<ScrollPanelWidget>("CHAT_SCROLLPANEL");
			chatTemplate = chatScrollPanel.Get<ContainerWidget>("CHAT_TEMPLATE");
			chatScrollPanel.RemoveChildren();
			chatScrollPanel.ScrollToBottom();

			foreach (var chatLine in orderManager.ChatCache)
				AddChatLine(chatLine.Color, chatLine.Name, chatLine.Text, true);

			orderManager.AddChatLine += AddChatLineWrapper;

			chatText.IsDisabled = () => world.IsReplay && !Game.Settings.Debug.EnableDebugCommandsInReplays;

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

			MiniYaml yaml;
			if (logicArgs.TryGetValue("ChatLineSound", out yaml))
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
		}

		public void AddChatLineWrapper(Color c, string from, string text)
		{
			if (chatOverlayDisplay != null)
				chatOverlayDisplay.AddLine(c, from, text);

			// HACK: Force disable the chat notification sound for the in-menu chat dialog
			// This works around our inability to disable the sounds for the in-game dialog when it is hidden
			AddChatLine(c, from, text, chatOverlay == null);
		}

		void AddChatLine(Color c, string from, string text, bool suppressSound)
		{
			var template = chatTemplate.Clone();
			var nameLabel = template.Get<LabelWidget>("NAME");
			var textLabel = template.Get<LabelWidget>("TEXT");

			var name = "";
			if (!string.IsNullOrEmpty(from))
				name = from + ":";

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var nameSize = font.Measure(from);

			nameLabel.GetColor = () => c;
			nameLabel.GetText = () => name;
			nameLabel.Bounds.Width = nameSize.X;
			textLabel.Bounds.X += nameSize.X;
			textLabel.Bounds.Width -= nameSize.X;

			// Hack around our hacky wordwrap behavior: need to resize the widget to fit the text
			text = WidgetUtils.WrapText(text, textLabel.Bounds.Width, font);
			textLabel.GetText = () => text;
			var dh = font.Measure(text).Y - textLabel.Bounds.Height;
			if (dh > 0)
			{
				textLabel.Bounds.Height += dh;
				template.Bounds.Height += dh;
			}

			var scrolledToBottom = chatScrollPanel.ScrolledToBottom;
			chatScrollPanel.AddChild(template);
			if (scrolledToBottom)
				chatScrollPanel.ScrollToBottom(smooth: true);

			if (!suppressSound)
				Game.Sound.PlayNotification(modRules, null, "Sounds", chatLineSound, null);
		}

		bool disposed = false;
		protected override void Dispose(bool disposing)
		{
			if (!disposed)
			{
				orderManager.AddChatLine -= AddChatLineWrapper;
				disposed = true;
			}

			base.Dispose(disposing);
		}
	}
}
