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

using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Meebey.SmartIrc4net;
using OpenRA.Primitives;

namespace OpenRA.Chat
{
	public enum ChatConnectionStatus { Disconnected, Connecting, Connected, Disconnecting, Joined, Error }
	public enum ChatMessageType { Message, Notification }

	public sealed class ChatUser
	{
		public readonly string Name;
		public bool IsOp;
		public bool IsVoiced;

		public ChatUser(string name, bool isOp, bool isVoice)
		{
			Name = name;
			IsOp = isOp;
			IsVoiced = isVoice;
		}
	}

	public sealed class ChatMessage
	{
		static long nextUID;

		public readonly DateTime Time;
		public readonly ChatMessageType Type;
		public readonly string Nick;
		public readonly string Message;
		public readonly string UID;

		public ChatMessage(DateTime time, ChatMessageType type, string nick, string message)
		{
			Time = time;
			Type = type;
			Nick = nick;
			Message = message;

			UID = Interlocked.Increment(ref nextUID).ToString();
		}

		public override string ToString()
		{
			var time = Time.ToString(Game.Settings.Chat.TimestampFormat);
			if (Type == ChatMessageType.Notification)
				return "{0} {1}".F(time, Message);

			return "{0} {1}: {2}".F(time, Nick, Message);
		}
	}

	public sealed class GlobalChat : IDisposable
	{
		readonly IrcClient client = new IrcClient();
		volatile Channel channel;

		public readonly ObservableSortedDictionary<string, ChatUser> Users = new ObservableSortedDictionary<string, ChatUser>(StringComparer.InvariantCultureIgnoreCase);
		public readonly ObservableList<ChatMessage> History = new ObservableList<ChatMessage>();

		volatile string topic;
		public string Topic { get { return topic; } }

		volatile ChatConnectionStatus connectionStatus = ChatConnectionStatus.Disconnected;
		public ChatConnectionStatus ConnectionStatus { get { return connectionStatus; } }

		public GlobalChat()
		{
			client.Encoding = System.Text.Encoding.UTF8;
			client.SendDelay = 100;
			client.ActiveChannelSyncing = true;

			client.OnConnecting += OnConnecting;
			client.OnConnected += OnConnected;
			client.OnDisconnecting += OnDisconnecting;
			client.OnDisconnected += OnDisconnected;
			client.OnError += OnError;
			client.OnKick += OnKick;

			client.OnRawMessage += (_, e) => Game.RunAfterTick(() => Log.Write("irc", e.Data.RawMessage));
			client.OnJoin += OnJoin;
			client.OnChannelActiveSynced += OnChannelActiveSynced;
			client.OnTopic += (_, e) => topic = e.Topic;
			client.OnTopicChange += (_, e) => topic = e.NewTopic;
			client.OnNickChange += OnNickChange;

			client.OnChannelMessage += (_, e) => AddMessage(e.Data.Nick, e.Data.Message);
			client.OnOp += (_, e) => SetUserOp(e.Whom, true);
			client.OnDeop += (_, e) => SetUserOp(e.Whom, false);
			client.OnVoice += (_, e) => SetUserVoiced(e.Whom, true);
			client.OnDevoice += (_, e) => SetUserVoiced(e.Whom, false);
			client.OnPart += OnPart;
			client.OnQuit += OnQuit;

			TrySetNickname(Game.Settings.Player.Name);
		}

		void SetUserOp(string whom, bool isOp)
		{
			Game.RunAfterTick(() =>
			{
				ChatUser user;
				if (Users.TryGetValue(whom, out user))
					user.IsOp = isOp;
			});
		}

		void SetUserVoiced(string whom, bool isVoiced)
		{
			Game.RunAfterTick(() =>
			{
				ChatUser user;
				if (Users.TryGetValue(whom, out user))
					user.IsVoiced = isVoiced;
			});
		}

		public void Connect()
		{
			if (client.IsConnected)
				return;

			new Thread(() =>
			{
				try
				{
					client.Connect(Game.Settings.Chat.Hostname, Game.Settings.Chat.Port);
				}
				catch (Exception e)
				{
					connectionStatus = ChatConnectionStatus.Error;
					AddNotification(e.Message);
					Game.RunAfterTick(() => Log.Write("irc", e.ToString()));

					return;
				}

				client.Listen();
			}) { Name = "IrcListenThread", IsBackground = true }.Start();
		}

		void AddNotification(string text)
		{
			var message = new ChatMessage(DateTime.Now, ChatMessageType.Notification, null, text);
			Game.RunAfterTick(() =>
			{
				History.Add(message);
				Log.Write("irc", text);
			});
		}

		void AddMessage(string nick, string text)
		{
			var message = new ChatMessage(DateTime.Now, ChatMessageType.Message, nick, text);
			Game.RunAfterTick(() =>
			{
				History.Add(message);
				Log.Write("irc", text);
			});
		}

		void OnConnecting(object sender, EventArgs e)
		{
			AddNotification("Connecting to {0}:{1}...".F(Game.Settings.Chat.Hostname, Game.Settings.Chat.Port));
			connectionStatus = ChatConnectionStatus.Connecting;
		}

		void OnConnected(object sender, EventArgs e)
		{
			AddNotification("Connected.");
			connectionStatus = ChatConnectionStatus.Connected;

			// Guard against settings.yaml modification
			var nick = SanitizedName(Game.Settings.Chat.Nickname);
			if (nick != Game.Settings.Chat.Nickname)
				Game.RunAfterTick(() => Game.Settings.Chat.Nickname = nick);

			client.Login(nick, "in-game IRC client", 0, "OpenRA");
			client.RfcJoin("#" + Game.Settings.Chat.Channel);
		}

		void OnDisconnecting(object sender, EventArgs e)
		{
			if (connectionStatus != ChatConnectionStatus.Error)
				connectionStatus = ChatConnectionStatus.Disconnecting;
		}

		void OnDisconnected(object sender, EventArgs e)
		{
			Game.RunAfterTick(Users.Clear);

			// Keep the chat window open if there is an error
			// It will be cleared by the Disconnect button
			if (connectionStatus != ChatConnectionStatus.Error)
			{
				Game.RunAfterTick(History.Clear);
				topic = null;
				connectionStatus = ChatConnectionStatus.Disconnected;
			}
		}

		void OnError(object sender, ErrorEventArgs e)
		{
			// Ignore any errors that happen during disconnect
			if (connectionStatus != ChatConnectionStatus.Disconnecting)
			{
				connectionStatus = ChatConnectionStatus.Error;
				AddNotification("Error: " + e.ErrorMessage);
			}
		}

		void OnKick(object sender, KickEventArgs e)
		{
			if (e.Whom == client.Nickname)
			{
				Disconnect();
				connectionStatus = ChatConnectionStatus.Error;
				AddNotification("You were kicked from the chat by {0}. ({1})".F(e.Who, e.KickReason));
			}
			else
			{
				Users.Remove(e.Whom);
				AddNotification("{0} was kicked from the chat by {1}. ({2})".F(e.Whom, e.Who, e.KickReason));
			}
		}

		void OnJoin(object sender, JoinEventArgs e)
		{
			if (e.Who == client.Nickname || channel == null || e.Channel != channel.Name)
				return;

			AddNotification("{0} joined the chat.".F(e.Who));
			Game.RunAfterTick(() => Users.Add(e.Who, new ChatUser(e.Who, false, false)));
		}

		void OnChannelActiveSynced(object sender, IrcEventArgs e)
		{
			channel = client.GetChannel(e.Data.Channel);
			AddNotification("{0} users online".F(channel.Users.Count));
			connectionStatus = ChatConnectionStatus.Joined;

			foreach (DictionaryEntry user in channel.Users)
			{
				var u = (ChannelUser)user.Value;
				Game.RunAfterTick(() => Users.Add(u.Nick, new ChatUser(u.Nick, u.IsOp, u.IsVoice)));
			}
		}

		void OnNickChange(object sender, NickChangeEventArgs e)
		{
			AddNotification("{0} is now known as {1}.".F(e.OldNickname, e.NewNickname));

			Game.RunAfterTick(() =>
			{
				ChatUser user;
				if (!Users.TryGetValue(e.OldNickname, out user))
					return;

				Users.Remove(e.OldNickname);
				Users.Add(e.NewNickname, new ChatUser(e.NewNickname, user.IsOp, user.IsVoiced));
			});
		}

		void OnQuit(object sender, QuitEventArgs e)
		{
			AddNotification("{0} left the chat.".F(e.Who));
			Game.RunAfterTick(() => Users.Remove(e.Who));
		}

		void OnPart(object sender, PartEventArgs e)
		{
			if (channel == null || e.Data.Channel != channel.Name)
				return;

			AddNotification("{0} left the chat.".F(e.Who));
			Game.RunAfterTick(() => Users.Remove(e.Who));
		}

		public string SanitizedName(string dirty)
		{
			if (string.IsNullOrEmpty(dirty))
				return null;

			// There is no need to mangle the nick if it is already valid
			if (Rfc2812.IsValidNickname(dirty))
				return dirty;

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

		public bool IsValidNickname(string name)
		{
			return Rfc2812.IsValidNickname(name);
		}

		public void SendMessage(string text)
		{
			if (connectionStatus != ChatConnectionStatus.Joined)
				return;

			// Guard against a last-moment disconnection
			try
			{
				client.SendMessage(SendType.Message, channel.Name, text);
				AddMessage(client.Nickname, text);
			}
			catch (NotConnectedException) { }
		}

		public bool TrySetNickname(string nick)
		{
			// TODO: This is inconsistent with the other check
			if (Rfc2812.IsValidNickname(nick))
			{
				client.RfcNick(nick);
				Game.Settings.Chat.Nickname = nick;
				return true;
			}

			return false;
		}

		public void Disconnect()
		{
			// Error is an alias for disconnect, but keeps the panel open
			// so that clients can see the error
			if (connectionStatus == ChatConnectionStatus.Error)
			{
				Game.RunAfterTick(History.Clear);
				topic = null;
				connectionStatus = ChatConnectionStatus.Disconnected;
			}
			else
				connectionStatus = ChatConnectionStatus.Disconnecting;

			if (!client.IsConnected)
				return;

			client.RfcQuit(Game.Settings.Chat.QuitMessage);

			AddNotification("Disconnecting from {0}...".F(client.Address));

			Game.RunAfterTick(() => Game.Settings.Chat.ConnectAutomatically = false);
		}

		public void Dispose()
		{
			// HACK: The IRC library we are using has terrible thread-handling code that relies on Thread.Abort.
			// There is a thread reading from the network socket which is aborted, however on Windows this is inside
			// native code so this abort call hangs until the network socket reads something and returns to managed
			// code where it can then be aborted.
			//
			// This means we may hang for several seconds during shutdown (until we receive something over IRC!) before
			// closing.
			//
			// Since our IRC client currently lives forever, the only time we call this Dispose method is during the
			// shutdown of our process. Therefore, we can work around the problem by just not bothering to disconnect
			// properly. Since our process is about to die anyway, it's not like anyone will care.
			////if (client.IsConnected)
			////	client.Disconnect();
		}
	}
}