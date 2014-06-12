#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Irc;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class IrcLogic
	{
		TextFieldWidget inputBox;
		TextFieldWidget nicknameBox;
		Widget connectBG;
		Widget ircContainer;

		[ObjectCreator.UseCtor]
		public IrcLogic(Widget widget)
		{
			var historyPanel = widget.Get<ScrollPanelWidget>("HISTORY_PANEL");
			var historyTemplate = widget.Get<LabelWidget>("HISTORY_TEMPLATE");
			var nicknamePanel = widget.Get<ScrollPanelWidget>("NICKNAME_PANEL");
			var nicknameTemplate = widget.Get<LabelWidget>("NICKNAME_TEMPLATE");

			inputBox = widget.Get<TextFieldWidget>("INPUT_BOX");
			inputBox.OnEnterKey = EnterPressed;
			inputBox.OnTabKey = TabPressed;
			inputBox.IsDisabled = () => IrcClient.Instance.GetChannel(IrcClient.MainChannel) == null;

			nicknameBox = widget.Get<TextFieldWidget>("NICKNAME_BOX");
			nicknameBox.Text = ChooseNickname(Game.Settings.Irc.Nickname);

			connectBG = widget.Get("IRC_CONNECT_BG");
			ircContainer = widget.Get("IRC_CONTAINER");

			widget.Get<ButtonWidget>("DISCONNECT_BUTTON").OnClick = IrcClient.Instance.Disconnect;

			MaybeShowConnectPanel();

			historyPanel.Bind(IrcClient.Instance.History, item => MakeLabelWidget(historyTemplate, item), LabelItemEquals, true);

			var mainChannel = IrcClient.Instance.GetChannel(IrcClient.MainChannel);
			if (mainChannel != null)
				nicknamePanel.Bind(mainChannel.Users, item => MakeLabelWidget(nicknameTemplate, item), LabelItemEquals, false);

			IrcClient.Instance.OnSync += l =>
			{
				var channel = l.GetChannel();
				if (channel.Name.EqualsIC(IrcClient.MainChannel))
					nicknamePanel.Bind(channel.Users, item => MakeLabelWidget(nicknameTemplate, item), LabelItemEquals, false);
			};
			IrcClient.Instance.OnKick += l =>
			{
				if (l.KickeeNickname.EqualsIC(IrcClient.Instance.LocalUser.Nickname) && l.Target.EqualsIC(IrcClient.MainChannel))
					nicknamePanel.Unbind();
			};
			IrcClient.Instance.OnPart += l =>
			{
				if (l.PrefixIsSelf() && l.Target.EqualsIC(IrcClient.MainChannel))
					nicknamePanel.Unbind();
			};
			IrcClient.Instance.OnDisconnect += () =>
			{
				nicknamePanel.Unbind();
				MaybeShowConnectPanel();
			};

			commands.Add("me", args =>
			{
				IrcClient.Instance.Act(IrcClient.MainChannel, args);
				IrcClient.AddAction(IrcClient.Instance.LocalUser.Nickname, args);
			});
			commands.Add("slap", args =>
			{
				IrcClient.Instance.Act(IrcClient.MainChannel, "slaps {0} around a bit with a large trout".F(args));
				IrcClient.AddAction(IrcClient.Instance.LocalUser.Nickname, "slaps {0} around a bit with a large trout".F(args));
			});
			commands.Add("notice", args =>
			{
				var split = args.Split(new[] { ' ' }, 2);
				if (split.Length < 2)
				{
					IrcClient.AddHistory("/notice: Not enough arguments");
					return;
				}
				IrcClient.Instance.Notice(split[0], split[1]);
				IrcClient.AddSelfNotice(split[0], split[1]);
			});
			commands.Add("disconnect", args =>
			{
				Game.Settings.Irc.ConnectAutomatically = false;
				Game.Settings.Save();
				IrcClient.Instance.Disconnect();
			});
			commands.Add("quit", args =>
			{
				Game.Settings.Irc.ConnectAutomatically = false;
				Game.Settings.Save();
				if (IrcClient.Instance.IsConnected)
					IrcClient.Instance.Quit(args);
				else
					IrcClient.Instance.Disconnect();
			});
			commands.Add("nick", args => IrcClient.Instance.SetNickname(args));
			commands.Add("topic", args => IrcClient.Instance.GetTopic(IrcClient.MainChannel));
		}

		void MaybeShowConnectPanel()
		{
			if (IrcClient.Instance.IsConnected || IrcClient.Instance.IsReconnecting)
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
			var connectAutomaticallyChecked = false;
			connectAutomaticallyCheckBox.IsChecked = () => connectAutomaticallyChecked;
			connectAutomaticallyCheckBox.OnClick = () => connectAutomaticallyChecked ^= true;

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

		static string ChooseNickname(string nickname)
		{
			if (!IrcUtils.IsNickname(nickname))
			{
				nickname = Game.Settings.Player.Name;
				if (!IrcUtils.IsNickname(nickname))
					nickname = Game.Settings.Irc.DefaultNickname;
			}
			return nickname;
		}

		void Connect()
		{
			var nickname = ChooseNickname(nicknameBox.Text);
			var s = Game.Settings.Irc;
			s.Nickname = nickname;
			Game.Settings.Save();
			IrcClient.Instance.Connect(s.Hostname, s.Port, s.ConnectionTimeout, nickname, s.Username ?? nickname, s.Realname ?? nickname);
		}

		static Widget MakeLabelWidget(LabelWidget template, object item)
		{
			var itemString = item.ToString();
			var widget = (LabelWidget)template.Clone();
			var font = Game.Renderer.Fonts[widget.Font];
			itemString = WidgetUtils.WrapText(itemString, widget.Bounds.Width, font);
			widget.Bounds.Height = font.Measure(itemString).Y;
			widget.GetText = () => itemString;
			return widget;
		}

		bool LabelItemEquals(Widget widget, object item)
		{
			return item != null && ((LabelWidget)widget).GetText() == item.ToString();
		}

		bool EnterPressed()
		{
			if (!inputBox.Text.Any())
				return true;

			var text = inputBox.Text;
			inputBox.Text = "";

			if (text[0] == '/')
			{
				var parts = text.Split(new[] { ' ' }, 2);
				var name = parts[0].Substring(1);
				var args = parts.Length > 1 ? parts[1] : null;

				Action<string> command;
				if (!commands.TryGetValue(name, out command))
				{
					IrcClient.AddHistory("{0}: Unknown command".F(name));
					return true;
				}
				command(args);
			}
			else
			{
				IrcClient.Instance.Message(IrcClient.MainChannel, text);
				IrcClient.AddMessage(IrcClient.Instance.LocalUser.Nickname, text);
			}
			return true;
		}

		Dictionary<string, Action<string>> commands = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);

		List<string> tabMatches = new List<string>();
		int tabMatchesIndex = -1;

		bool TabPressed()
		{
			if (!inputBox.Text.Any())
				return true;

			var channel = IrcClient.Instance.GetChannel(IrcClient.MainChannel);

			if (channel == null)
				return true;

			var spaceIndex = inputBox.Text.TrimEnd().LastIndexOf(' ');
			var tabMatchtext = inputBox.Text.Substring(spaceIndex + 1);

			if (tabMatchesIndex < 0 || !tabMatches.Any() || tabMatchtext != tabMatches[tabMatchesIndex])
				tabMatches = channel.Users.Keys.Where(u => u.StartsWith(tabMatchtext, StringComparison.OrdinalIgnoreCase)).ToList();

			if (!tabMatches.Any())
				return true;

			tabMatchesIndex = (tabMatchesIndex + 1) % tabMatches.Count;
			inputBox.Text = inputBox.Text.Remove(spaceIndex + 1) + tabMatches[tabMatchesIndex];
			inputBox.CursorPosition = inputBox.Text.Length;

			return true;
		}
	}
}
