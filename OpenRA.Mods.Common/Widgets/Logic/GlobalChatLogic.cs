#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
		readonly LabelWidget historyTemplate;
		readonly ScrollPanelWidget nicknamePanel;
		readonly Widget nicknameTemplate;
		readonly TextFieldWidget inputBox;

		[ObjectCreator.UseCtor]
		public GlobalChatLogic(Widget widget)
		{
			historyPanel = widget.Get<ScrollPanelWidget>("HISTORY_PANEL");
			historyTemplate = historyPanel.Get<LabelWidget>("HISTORY_TEMPLATE");
			nicknamePanel = widget.Get<ScrollPanelWidget>("NICKNAME_PANEL");
			nicknameTemplate = nicknamePanel.Get("NICKNAME_TEMPLATE");

			historyPanel.Bind(Game.GlobalChat.History, MakeHistoryWidget, HistoryWidgetEquals, true);
			nicknamePanel.Bind(Game.GlobalChat.Users, MakeUserWidget, UserWidgetEquals, false);

			inputBox = widget.Get<TextFieldWidget>("CHAT_TEXTFIELD");
			inputBox.IsDisabled = () => Game.GlobalChat.ConnectionStatus != ChatConnectionStatus.Joined;
			inputBox.OnEnterKey = EnterPressed;

			// Set a random default nick
			if (Game.Settings.Chat.Nickname == new ChatSettings().Nickname)
				Game.Settings.Chat.Nickname += Game.CosmeticRandom.Next(100, 999);

			var nicknameBox = widget.Get<TextFieldWidget>("NICKNAME_TEXTFIELD");
			nicknameBox.Text = Game.GlobalChat.SanitizedName(Game.Settings.Chat.Nickname);
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
			connectButton.OnClick = () =>
			{
				Game.Settings.Chat.Nickname = nicknameBox.Text;
				Game.Settings.Save();
				Game.GlobalChat.Connect();
			};

			var mainPanel = widget.Get("GLOBALCHAT_MAIN_PANEL");
			mainPanel.IsVisible = () => Game.GlobalChat.ConnectionStatus != ChatConnectionStatus.Disconnected;

			mainPanel.Get<LabelWidget>("CHANNEL_TOPIC").GetText = () => Game.GlobalChat.Topic;

			if (Game.Settings.Chat.ConnectAutomatically && Game.GlobalChat.IsValidNickname(Game.Settings.Chat.Nickname))
				Game.GlobalChat.Connect();
		}

		Widget MakeHistoryWidget(object o)
		{
			var message = (ChatMessage)o;
			var widget = (LabelWidget)historyTemplate.Clone();
			var font = Game.Renderer.Fonts[widget.Font];

			var color = message.Type == ChatMessageType.Notification ?
				ChromeMetrics.Get<Color>("GlobalChatNotificationColor") :
				ChromeMetrics.Get<Color>("GlobalChatTextColor");

			var display = WidgetUtils.WrapText(message.ToString(), widget.Bounds.Width, font);
			widget.Bounds.Height = font.Measure(display).Y;
			widget.GetText = () => display;
			widget.GetColor = () => color;
			widget.Id = message.UID;
			return widget;
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
