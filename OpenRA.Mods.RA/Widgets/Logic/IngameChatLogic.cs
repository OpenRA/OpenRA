#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngameChatLogic
	{
		readonly Ruleset modRules;

		readonly ContainerWidget chatOverlay;
		readonly ChatDisplayWidget chatOverlayDisplay;

		readonly ContainerWidget chatChrome;
		readonly ScrollPanelWidget chatScrollPanel;
		readonly ContainerWidget chatTemplate;
		readonly TextFieldWidget chatText;

		readonly List<INotifyChat> chatTraits;

		readonly TabCompletionLogic tabCompletion = new TabCompletionLogic();

		bool teamChat;

		[ObjectCreator.UseCtor]
		public IngameChatLogic(Widget widget, OrderManager orderManager, World world, Ruleset modRules)
		{
			this.modRules = modRules;

			chatTraits = world.WorldActor.TraitsImplementing<INotifyChat>().ToList();

			var players = world.Players.Where(p => p != world.LocalPlayer && !p.NonCombatant && !p.IsBot);
			var disableTeamChat = world.LocalPlayer == null || world.LobbyInfo.IsSinglePlayer || !players.Any(p => p.IsAlliedWith(world.LocalPlayer));
			teamChat = !disableTeamChat;

			tabCompletion.Commands = chatTraits.OfType<ChatCommands>().SelectMany(x => x.Commands.Keys).ToList();
			tabCompletion.Names = orderManager.LobbyInfo.Clients.Select(c => c.Name).Distinct().ToList();

			var chatPanel = (ContainerWidget)widget;
			chatOverlay = chatPanel.Get<ContainerWidget>("CHAT_OVERLAY");
			chatOverlayDisplay = chatOverlay.Get<ChatDisplayWidget>("CHAT_DISPLAY");
			chatOverlay.Visible = false;

			chatChrome = chatPanel.Get<ContainerWidget>("CHAT_CHROME");
			chatChrome.Visible = true;

			var chatMode = chatChrome.Get<ButtonWidget>("CHAT_MODE");
			chatMode.GetText = () => teamChat ? "Team" : "All";
			chatMode.OnClick = () => teamChat ^= true;
			chatMode.IsDisabled = () => disableTeamChat;

			chatText = chatChrome.Get<TextFieldWidget>("CHAT_TEXTFIELD");
			chatText.OnAltKey = () =>
			{
				if (!disableTeamChat)
					teamChat ^= true;
				return true;
			};
			chatText.OnEnterKey = () =>
			{
				var team = teamChat && !disableTeamChat;
				if (chatText.Text != "")
					if (!chatText.Text.StartsWith("/"))
						orderManager.IssueOrder(Order.Chat(team, chatText.Text.Trim()));
					else
						if (chatTraits != null)
						{
							var text = chatText.Text.Trim();
							foreach (var trait in chatTraits)
								trait.OnChat(orderManager.LocalClient.Name, text);
						}

				CloseChat();
				return true;
			};
			chatText.OnTabKey = () =>
			{
				chatText.Text = tabCompletion.Complete(chatText.Text);
				chatText.CursorPosition = chatText.Text.Length;
				return true;
			};

			chatText.OnEscKey = () => { CloseChat(); return true; };

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

			chatScrollPanel = chatChrome.Get<ScrollPanelWidget>("CHAT_SCROLLPANEL");
			chatTemplate = chatScrollPanel.Get<ContainerWidget>("CHAT_TEMPLATE");
			chatScrollPanel.RemoveChildren();

			Game.AddChatLine += AddChatLine;
			Game.BeforeGameStart += UnregisterEvents;

			CloseChat();
		}

		void UnregisterEvents()
		{
			Game.AddChatLine -= AddChatLine;
			Game.BeforeGameStart -= UnregisterEvents;
		}

		public void OpenChat()
		{
			chatText.Text = "";
			chatOverlay.Visible = false;
			chatChrome.Visible = true;
			chatScrollPanel.ScrollToBottom();
			chatText.TakeKeyboardFocus();
		}

		public void CloseChat()
		{
			chatOverlay.Visible = true;
			chatChrome.Visible = false;
			chatText.YieldKeyboardFocus();
		}

		public void AddChatLine(Color c, string from, string text)
		{
			chatOverlayDisplay.AddLine(c, from, text);

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

			Sound.PlayNotification(modRules, null, "Sounds", "ChatLine", null);
		}
	}
}
