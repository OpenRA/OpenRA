#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Chat;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class GlobalChatLogic : ChromeLogic
	{
		readonly ScrollPanelWidget historyPanel;
		readonly ContainerWidget chatTemplate;
		readonly ScrollPanelWidget nicknamePanel;
		readonly Widget nicknameTemplate;
		readonly TextFieldWidget inputBox;

		[ObjectCreator.UseCtor]
		public GlobalChatLogic(Widget widget)
		{
			historyPanel = widget.Get<ScrollPanelWidget>("HISTORY_PANEL");
			chatTemplate = historyPanel.Get<ContainerWidget>("CHAT_TEMPLATE");
			nicknamePanel = widget.Get<ScrollPanelWidget>("NICKNAME_PANEL");
			nicknameTemplate = nicknamePanel.Get("NICKNAME_TEMPLATE");

			var textColor = ChromeMetrics.Get<Color>("GlobalChatTextColor");
			var textLabel = chatTemplate.Get<LabelWidget>("TEXT");
			textLabel.GetColor = () => textColor;

			historyPanel.Bind(Game.GlobalChat.History, MakeHistoryWidget, HistoryWidgetEquals, true);
			nicknamePanel.Bind(Game.GlobalChat.Users, MakeUserWidget, UserWidgetEquals, false);

			inputBox = widget.Get<TextFieldWidget>("CHAT_TEXTFIELD");
			inputBox.IsDisabled = () => Game.GlobalChat.ConnectionStatus != ChatConnectionStatus.Joined;
			inputBox.OnEnterKey = EnterPressed;

			var nickName = Game.GlobalChat.SanitizedName(Game.Settings.Player.Name);
			var nicknameBox = widget.Get<TextFieldWidget>("NICKNAME_TEXTFIELD");
			nicknameBox.Text = nickName;
			nicknameBox.OnTextEdited = () =>
			{
				nicknameBox.Text = Game.GlobalChat.SanitizedName(nicknameBox.Text);
			};

			var connectPanel = widget.Get("GLOBALCHAT_CONNECT_PANEL");
			connectPanel.IsVisible = () => Game.GlobalChat.ConnectionStatus == ChatConnectionStatus.Disconnected;

			var disconnectButton = widget.Get<ButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.OnClick = Game.GlobalChat.Disconnect;

			var connectAutomaticallyCheckBox = connectPanel.Get<CheckboxWidget>("CONNECT_AUTOMATICALLY_CHECKBOX");
			connectAutomaticallyCheckBox.IsChecked = () => Game.Settings.Chat.ConnectAutomatically;
			connectAutomaticallyCheckBox.OnClick = () => { Game.Settings.Chat.ConnectAutomatically ^= true; Game.Settings.Save(); };

			var connectButton = connectPanel.Get<ButtonWidget>("CONNECT_BUTTON");
			connectButton.IsDisabled = () => !Game.GlobalChat.IsValidNickname(nicknameBox.Text);
			connectButton.OnClick = () => Game.GlobalChat.Connect(nicknameBox.Text);

			var mainPanel = widget.Get("GLOBALCHAT_MAIN_PANEL");
			mainPanel.IsVisible = () => Game.GlobalChat.ConnectionStatus != ChatConnectionStatus.Disconnected;

			mainPanel.Get<LabelWidget>("CHANNEL_TOPIC").GetText = () => Game.GlobalChat.Topic;

			if (Game.Settings.Chat.ConnectAutomatically)
				Game.GlobalChat.Connect(nickName);
		}

		Widget MakeHistoryWidget(object o)
		{
			var message = (ChatMessage)o;
			var from = message.Type == ChatMessageType.Notification ? "Battlefield Control" : message.Nick;
			var color = message.Type == ChatMessageType.Notification ? ChromeMetrics.Get<Color>("GlobalChatNotificationColor")
				: ChromeMetrics.Get<Color>("GlobalChatPlayerNameColor");
			var template = (ContainerWidget)chatTemplate.Clone();
			LobbyUtils.SetupChatLine(template, color, from, message.Message);

			template.Id = message.UID;
			return template;
		}

		bool HistoryWidgetEquals(Widget widget, object o)
		{
			return ((LabelWidget)widget).Id == ((ChatMessage)o).UID;
		}

		Widget MakeUserWidget(object o)
		{
			var nick = (string)o;
			var client = Game.GlobalChat.Users[nick];

			var item = nicknameTemplate.Clone();
			item.Id = client.Name;
			item.IsVisible = () => true;
			var name = item.Get<LabelWidget>("NICK");
			name.GetText = () => client.Name;
			name.IsVisible = () => true;

			// TODO: Add custom image for voice
			var indicator = item.Get<ImageWidget>("INDICATOR");
			indicator.IsVisible = () => client.IsOp || client.IsVoiced;
			indicator.GetImageName = () => client.IsOp || client.IsVoiced ? "admin" : "";

			return item;
		}

		bool UserWidgetEquals(Widget widget, object o)
		{
			var nick = (string)o;
			return widget.Id == nick;
		}

		bool EnterPressed()
		{
			if (inputBox.Text.Length == 0)
				return true;

			if (inputBox.Text.StartsWith("/nick "))
			{
				var nick = inputBox.Text.Replace("/nick ", string.Empty);
				Game.GlobalChat.TrySetNickname(nick);
			}
			else
				Game.GlobalChat.SendMessage(inputBox.Text);

			inputBox.Text = "";

			return true;
		}

		bool disposed;
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposed)
				return;

			historyPanel.Unbind();
			nicknamePanel.Unbind();

			disposed = true;
		}
	}
}
