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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Meebey.SmartIrc4net;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class IrcLogic
	{
		readonly TextFieldWidget inputBox;
		readonly TextFieldWidget nicknameBox;
		readonly Widget connectBG;
		readonly Widget ircContainer;

		readonly ScrollPanelWidget historyPanel;
		readonly LabelWidget historyTemplate;

		readonly ScrollPanelWidget nicknamePanel;
		readonly LabelWidget nicknameTemplate;

		bool pingSent;
		Channel channel;

		[ObjectCreator.UseCtor]
		public IrcLogic(Widget widget)
		{
			Log.AddChannel("irc", "irc.log");

			historyPanel = widget.Get<ScrollPanelWidget>("HISTORY_PANEL");
			historyTemplate = widget.Get<LabelWidget>("HISTORY_TEMPLATE");
			nicknamePanel = widget.Get<ScrollPanelWidget>("NICKNAME_PANEL");
			nicknameTemplate = widget.Get<LabelWidget>("NICKNAME_TEMPLATE");

			inputBox = widget.Get<TextFieldWidget>("INPUT_BOX");
			inputBox.OnEnterKey = EnterPressed;
			inputBox.IsDisabled = () => Irc.Client == null;

			if (Game.Settings.Irc.Nickname == new IrcSettings().Nickname)
				Game.Settings.Irc.Nickname += Game.CosmeticRandom.Next(100, 999);

			nicknameBox = widget.Get<TextFieldWidget>("NICKNAME_BOX");
			nicknameBox.Text = SanitizedName(Game.Settings.Irc.Nickname);
			nicknameBox.OnTextEdited = () =>
			{
				nicknameBox.Text = SanitizedName(nicknameBox.Text);
				Game.Settings.Irc.Nickname = nicknameBox.Text;
				Game.Settings.Save();
			};

			connectBG = widget.Get("IRC_CONNECT_BG");
			ircContainer = widget.Get("IRC_CONTAINER");

			var disconnectButton = widget.Get<ButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.IsDisabled = () => Irc.Client == null;
			disconnectButton.OnClick = Disconnect;

			MaybeShowConnectPanel();
		}

		static string SanitizedName(string dirty)
		{
			if (string.IsNullOrEmpty(dirty))
				return null;

			// TODO: some special chars are allowed as well, but not at every position
			var clean = new string(dirty.Where(c => char.IsLetterOrDigit(c)).ToArray());

			if (string.IsNullOrEmpty(clean))
				return null;

			if (char.IsDigit(clean[0]))
				return SanitizedName(clean.Substring(1));

			// Source: https://tools.ietf.org/html/rfc2812#section-1.2.1
			if (clean.Length > 9)
				clean = clean.Substring(0, 9);

			return clean;
		}

		void MaybeShowConnectPanel()
		{
			if (Irc.Client != null && Irc.Client.IsConnected)
			{
				ircContainer.Visible = true;
				connectBG.Visible = false;

				Initialize();

				if (Irc.Client.JoinedChannels.Count > 0)
					channel = Irc.Client.GetChannel(Irc.Client.JoinedChannels[0]);

				SyncNicknamePanel();

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
			connectButton.IsDisabled = () => string.IsNullOrEmpty(nicknameBox.Text);
			connectButton.OnClick = () =>
			{
				ircContainer.Visible = true;
				connectBG.Visible = false;

				Game.Settings.Irc.ConnectAutomatically = connectAutomaticallyCheckBox.IsChecked();
				Game.Settings.Save();
				Connect();
			};
		}

		void Initialize()
		{
			Irc.Client.OnConnected += OnConnected;
			Irc.Client.OnError += OnError;
			Irc.Client.OnRawMessage += OnRawMessage;
			Irc.Client.OnJoin += OnJoin;
			Irc.Client.OnChannelActiveSynced += OnChannelActiveSynced;
			Irc.Client.OnNickChange += OnNickChange;
			Irc.Client.OnPart += OnPart;
			Irc.Client.OnQuit += OnQuit;
			Irc.Client.OnChannelMessage += OnChannelMessage;
			Irc.Client.OnPong += OnPong;
		}

		void Connect()
		{
			Irc.Client = new IrcClient();
			Irc.Client.Encoding = System.Text.Encoding.UTF8;
			Irc.Client.SendDelay = 100;
			Irc.Client.ActiveChannelSyncing = true;

			Initialize();

			Game.OnQuit += Disconnect;

			try
			{
				AddChatLine("Connecting to {0}...".F(Game.Settings.Irc.Hostname));
				Irc.Client.Connect(Game.Settings.Irc.Hostname, Game.Settings.Irc.Port);
			}
			catch (Exception e)
			{
				AddChatLine("Connection error: {0}".F(e.Message));
				Game.RunAfterTick(() =>
				{
					Log.Write("irc", e.ToString());
				});
			}

			new Thread(Irc.Client.Listen) { Name = "IrcListenThread" }.Start();
		}

		void OnPong(object sender, PongEventArgs e)
		{
			if (pingSent)
			{
				AddChatLine("PONG recieved after {0} ms.".F(e.Lag.Milliseconds));
				pingSent = false;
			}
			else
			{
				Game.RunAfterTick(() =>
				{
					Log.Write("irc", "PONG sent after {0} ms.".F(e.Lag.Milliseconds));
				});
			}
		}

		Widget MakeLabelWidget(LabelWidget template, string item)
		{
			var widget = (LabelWidget)template.Clone();
			var font = Game.Renderer.Fonts[widget.Font];
			item = WidgetUtils.WrapText(item, widget.Bounds.Width, font);
			widget.Bounds.Height = font.Measure(item).Y;
			widget.GetText = () => item;
			widget.Id = item;
			return widget;
		}

		void AddChatLine(string text)
		{
			Game.RunAfterTick(() =>
			{
				Log.Write("irc", text);

				var scrolledToBottom = historyPanel.ScrolledToBottom;

				var newChild = MakeLabelWidget(historyTemplate, text);
				historyPanel.AddChild(newChild);

				if (scrolledToBottom)
					historyPanel.ScrollToBottom(smooth: true);
			});
		}

		bool EnterPressed()
		{
			if (inputBox.Text.Length == 0)
				return true;

			var text = inputBox.Text;
			inputBox.Text = "";

			if (text.StartsWith("/nick "))
			{
				var nick = text.Replace("/nick ", string.Empty);
				if (Rfc2812.IsValidNickname(nick))
					Irc.Client.RfcNick(nick);
				else
					AddChatLine("Invalid nickname.");
			}
			else if (text.StartsWith("/ping "))
			{
				Irc.Client.RfcPing(Irc.Client.GetIrcUser(text.Replace("/ping ", string.Empty)).Host);
				pingSent = true;
			}
			else if (text.StartsWith("/"))
				AddChatLine("Unknown command.");
			else
			{
				AddChatLine("[{0}] <{1}> {2}".F(DateTime.Now.ToString(Game.Settings.Irc.TimestampFormat), Irc.Client.Nickname, text));
				Irc.Client.SendMessage(SendType.Message, "#" + Game.Settings.Irc.Channel, text);
			}

			return true;
		}

		void OnConnected(object sender, EventArgs e)
		{
			AddChatLine("Connected.");

			if (!Rfc2812.IsValidNickname(Game.Settings.Irc.Nickname))
			{
				AddChatLine("Invalid nickname. Can't login.");
				return;
			}

			Irc.Client.Login(new[] { Game.Settings.Irc.Nickname }, "in-game IRC client", 0, "OpenRA");

			Irc.Client.RfcJoin("#" + Game.Settings.Irc.Channel);
		}

		void OnError(object sender, ErrorEventArgs e)
		{
			AddChatLine("Error: " + e.ErrorMessage);
			Game.RunAfterTick(() =>
			{
				Log.Write("irc", e.ToString());
			});
		}

		void OnRawMessage(object sender, IrcEventArgs e)
		{
			Game.RunAfterTick(() =>
			{
				Log.Write("irc", e.Data.RawMessage);
			});
		}

		void OnChannelMessage(object sender, IrcEventArgs e)
		{
			AddChatLine("[{0}] <{1}> {2}".F(DateTime.Now.ToString(Game.Settings.Irc.TimestampFormat), e.Data.Nick, e.Data.Message));
		}

		void OnJoin(object sender, JoinEventArgs e)
		{
			if (e.Who == Irc.Client.Nickname)
				return;

			AddChatLine("{0} joined channel {1}.".F(e.Who, e.Channel));
			channel = Irc.Client.GetChannel(e.Channel);
			SyncNicknamePanel();
		}

		void OnChannelActiveSynced(object sender, IrcEventArgs e)
		{
			channel = Irc.Client.GetChannel(e.Data.Channel);

			AddChatLine("{0} users online".F(channel.Users.Count));

			if (!string.IsNullOrEmpty(channel.Topic))
				AddChatLine("*** Topic: {0}".F(channel.Topic));

			SyncNicknamePanel();
		}

		void OnNickChange(object sender, NickChangeEventArgs e)
		{
			AddChatLine("{0} is now known as {1}.".F(e.OldNickname, e.NewNickname));
			SyncNicknamePanel();
		}

		void SyncNicknamePanel()
		{
			if (channel == null)
				return;

			var users = channel.Users;
			Game.RunAfterTick(() =>
			{
				nicknamePanel.RemoveChildren();

				foreach (DictionaryEntry user in users)
				{
					var channeluser = (ChannelUser)user.Value;
					var prefix = channeluser.IsOp ? "@" : channeluser.IsVoice ? "+" : "";
					var newChild = MakeLabelWidget(nicknameTemplate, prefix + channeluser.Nick);
					nicknamePanel.AddChild(newChild);
				}
			});
		}

		void OnQuit(object sender, QuitEventArgs e)
		{
			AddChatLine("{0} quit.".F(e.Who));
		}

		void OnPart(object sender, PartEventArgs e)
		{
			AddChatLine("{0} left {1}.".F(e.Who, e.Data.Channel));
			channel = Irc.Client.GetChannel(e.Data.Channel);
			SyncNicknamePanel();
		}

		void Disconnect()
		{
			if (Irc.Client == null)
				return;

			Irc.Client.RfcQuit(Game.Settings.Irc.QuitMessage);

			AddChatLine("Disconnecting from {0}...".F(Irc.Client.Address));

			if (Irc.Client.IsConnected)
				Irc.Client.Disconnect();

			nicknamePanel.RemoveChildren();

			Game.Settings.Irc.ConnectAutomatically = false;

			Irc.Client = null;

			MaybeShowConnectPanel();
		}
	}
}
