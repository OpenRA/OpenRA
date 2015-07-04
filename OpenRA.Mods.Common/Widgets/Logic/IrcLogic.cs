#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using IrcDotNet;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class IrcLogic
	{
		StandardIrcClient client;

		TextFieldWidget inputBox;
		TextFieldWidget nicknameBox;
		Widget connectBG;
		Widget ircContainer;

		ScrollPanelWidget historyPanel;
		LabelWidget historyTemplate;
		ScrollPanelWidget nicknamePanel;
		LabelWidget nicknameTemplate;

		[ObjectCreator.UseCtor]
		public IrcLogic(Widget widget)
		{
			historyPanel = widget.Get<ScrollPanelWidget>("HISTORY_PANEL");
			historyTemplate = widget.Get<LabelWidget>("HISTORY_TEMPLATE");
			nicknamePanel = widget.Get<ScrollPanelWidget>("NICKNAME_PANEL");
			nicknameTemplate = widget.Get<LabelWidget>("NICKNAME_TEMPLATE");

			inputBox = widget.Get<TextFieldWidget>("INPUT_BOX");
			inputBox.OnEnterKey = EnterPressed;
			inputBox.IsDisabled = () => client == null;

			nicknameBox = widget.Get<TextFieldWidget>("NICKNAME_BOX");
			nicknameBox.Text = Game.Settings.Irc.Nickname + Game.CosmeticRandom.Next();

			connectBG = widget.Get("IRC_CONNECT_BG");
			ircContainer = widget.Get("IRC_CONTAINER");

			var disconnectButton = widget.Get<ButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.IsDisabled = () => client == null;
			disconnectButton.OnClick = Disconnect;

			MaybeShowConnectPanel();

			Log.AddChannel("irc", "irc.log");
		}

		void MaybeShowConnectPanel()
		{
			if (client != null && client.IsConnected)
			{
				ircContainer.Visible = true;
				connectBG.Visible = false;
				return;
			}

			if (Game.Settings.Irc.ConnectAutomatically)
			{
				ircContainer.Visible = true;
				connectBG.Visible = false;
				Connect();
				return;
			}

			ircContainer.Visible = false;
			connectBG.Visible = true;

			var connectAutomaticallyCheckBox = connectBG.Get<CheckboxWidget>("CONNECT_AUTOMATICALLY_CHECKBOX");
			connectAutomaticallyCheckBox.IsChecked = () => Game.Settings.Irc.ConnectAutomatically;
			connectAutomaticallyCheckBox.OnClick = () => Game.Settings.Irc.ConnectAutomatically ^= true;

			var connectButton = connectBG.Get<ButtonWidget>("CONNECT_BUTTON");
			connectButton.OnClick = () =>
			{
				ircContainer.Visible = true;
				connectBG.Visible = false;

				Game.Settings.Irc.ConnectAutomatically = connectAutomaticallyCheckBox.IsChecked();
				Game.Settings.Save();
				Connect();
			};
		}

		void Connect()
		{
			Game.RunAfterTick(() =>
			{
				client = new StandardIrcClient();
				client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
				client.Connected += ClientConnected;
				client.Disconnected += ClientDisconnected;
				client.Registered += ClientRegistered;
				client.ConnectFailed += ConnectFailed;
				client.Error += ClientError;

				var registrationInfo = new IrcUserRegistrationInfo() {
					NickName = nicknameBox.Text,
					UserName = Game.Settings.Irc.Username,
					RealName = Game.Settings.Irc.Realname
				};

				try
				{
					AddChatLine("Connecting to {0}...".F(Game.Settings.Irc.Hostname));
					client.Connect(Game.Settings.Irc.Hostname, false, registrationInfo);
				}
				catch (Exception e)
				{
					AddChatLine("Connection error: {0}".F(e.Message));
					Log.Write("irc", e.ToString());
				}
			});
		}

		Widget MakeLabelWidget(LabelWidget template, string item)
		{
			var widget = (LabelWidget)template.Clone();
			var font = Game.Renderer.Fonts[widget.Font];
			item = WidgetUtils.WrapText(item, widget.Bounds.Width, font);
			widget.Bounds.Height = font.Measure(item).Y;
			widget.GetText = () => item;
			return widget;
		}

		void AddChatLine(string text)
		{
			Log.Write("irc", text);

			var scrolledToBottom = historyPanel.ScrolledToBottom;
			historyPanel.AddChild(MakeLabelWidget(historyTemplate, text));
			if (scrolledToBottom)
				historyPanel.ScrollToBottom(smooth: true);
		}

		bool EnterPressed()
		{
			if (!inputBox.Text.Any())
				return true;

			var text = inputBox.Text;
			inputBox.Text = "";

			if (text.StartsWith("/"))
				AddChatLine("Unknown command");
			else
				Game.RunAfterTick(() =>
				{
					AddChatLine("[{0}] <{1}> {2}".F(DateTime.Now.ToString(Game.Settings.Irc.TimestampFormat), client.LocalUser.NickName, text));
					client.LocalUser.SendMessage(client.Channels.First(), text);
				});

			return true;
		}

		public void Disconnect()
		{
			Game.RunAfterTick(() =>
			{
				Game.Settings.Irc.ConnectAutomatically = false;
				var serverName = client.ServerName;
				client.Quit(Game.Settings.Irc.ConnectionTimeout, Game.Settings.Irc.QuitMessage);
				AddChatLine("Disconnecting from {0}...".F(serverName));
			});
		}

		void ClientConnected(object sender, EventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				AddChatLine("Connected.");
			});
		}

		void ClientDisconnected(object sender, EventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				AddChatLine("Disconnected.");

				if (client != null)
				{
					client.Dispose();
					client = null;
				}

				MaybeShowConnectPanel();
			});
		}

		void ClientRegistered(object sender, EventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				var client = (IrcClient)sender;

				client.LocalUser.MessageReceived += MessageReceived;
				client.LocalUser.JoinedChannel += JoinedChannel;
				client.LocalUser.LeftChannel += LeftChannel;

				Log.Write("irc", "Registered client.");

				client.Channels.Join("#" + Game.Settings.Irc.Channel);
				AddChatLine("Joining channel #{0}...".F(Game.Settings.Irc.Channel));
			});
		}

		void NoticeReceived(object sender, IrcMessageEventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				AddChatLine("[{0}] NOTICE -{1}- {2}".F(DateTime.Now.ToString(Game.Settings.Irc.TimestampFormat), e.Source, e.Text));
			});
		}

		void MessageReceived(object sender, IrcMessageEventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				AddChatLine("[{0}] <{1}> {2}".F(DateTime.Now.ToString(Game.Settings.Irc.TimestampFormat), e.Source.Name, e.Text));
			});
		}

		void JoinedChannel(object sender, IrcChannelEventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				AddChatLine("Channel joined.");

				e.Channel.TopicChanged += TopicChanged;
				e.Channel.UserJoined += UserJoined;
				e.Channel.UserLeft += UserLeft;
				e.Channel.MessageReceived += MessageReceived;
				e.Channel.UsersListReceived += UsersListReceived;
			});
		}

		void TopicChanged(object sender, IrcUserEventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				AddChatLine("*** Topic: {0}".F(client.Channels.First().Topic));
			});
		}

		void UsersListReceived(object sender, EventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				foreach (var user in client.Channels.First().Users)
					nicknamePanel.AddChild(MakeLabelWidget(nicknameTemplate, user.User.NickName));
			});
		}

		void LeftChannel(object sender, IrcChannelEventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				AddChatLine("Channel left");

				e.Channel.UserJoined -= UserJoined;
				e.Channel.UserLeft -= UserLeft;
				e.Channel.MessageReceived -= MessageReceived;
			});
		}

		void UserLeft(object sender, IrcChannelUserEventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				var name = ((IrcChannel)sender).Name;
				AddChatLine("{0} left #{1}".F(e.ChannelUser, name));
				nicknamePanel.RemoveChild(nicknamePanel.Children.OfType<LabelWidget>().First(f => f.Text == name));
			});
		}

		void UserJoined(object sender, IrcChannelUserEventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				var nickName = e.ChannelUser.User.NickName;
				AddChatLine("{0} joined.".F(nickName));
				nicknamePanel.AddChild(MakeLabelWidget(nicknameTemplate, nickName));
			});
		}

		void ConnectFailed(object sender, IrcErrorEventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				AddChatLine("Connection failed: {0}".F(e.Error.Message));
				Log.Write("irc", e.Error.ToString());
			});
		}

		void ClientError(object sender, IrcErrorEventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				AddChatLine("Client error: {0}".F(e.Error.Message));
				Log.Write("irc", e.Error.ToString());
			});
		}
	}
}
