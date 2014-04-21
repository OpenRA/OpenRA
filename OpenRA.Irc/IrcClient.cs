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
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using OpenRA.Primitives;

namespace OpenRA.Irc
{
	public class IrcClient : IDisposable
	{
		public static readonly IrcClient Instance = new IrcClient();

		public static string MainChannel { get { return '#' + Game.Settings.Irc.Channel; } }

		public static void AddHistory(string line)
		{
			Instance.History.Add("{0} {1}".F(DateTime.Now.ToString(Game.Settings.Irc.TimestampFormat), line));
		}

		public static void AddMessage(string nickname, string message)
		{
			AddHistory("{0}: {1}".F(nickname, message));
		}

		public static void AddNotice(string nickname, string message)
		{
			AddHistory("-{0}- {1}".F(nickname, message));
		}

		public static void AddSelfNotice(string nickname, string message)
		{
			AddHistory("-> -{0}- {1}".F(nickname, message));
		}

		public static void AddAction(string nickname, string message)
		{
			AddHistory("* {0} {1}".F(nickname, message));
		}

		static void InstanceInitialize()
		{
			var s = Game.Settings.Irc;

			Instance.OnPublicMessage += l =>
			{
				var action = IrcUtils.FromAction(l.Message);
				if (action != null)
					AddAction(l.Prefix.Nickname, action);
				else
					AddMessage(l.Prefix.Nickname, l.Message);
			};
			Instance.OnPrivateMessage += l =>
			{
				var ctcp = IrcUtils.FromCtcp(l.Message);
				if (ctcp == null)
					return;

				var split = ctcp.Split(new[] { ' ' }, 2);
				var command = split[0];
				if (command.EqualsIC("VERSION"))
				{
					var mod = Game.modData.Manifest.Mod;
					Instance.CtcpRespond(l.Prefix.Nickname, command, "{0}: {1}".F(mod.Title, mod.Version));
				}
			};
			Instance.OnPrivateNotice += l =>
			{
				if (l.Target == "*") // Drop pre-register notices
					return;
				AddNotice(l.Prefix.Nickname, l.Message);
			};
			Instance.OnRegister += l =>
			{
				Instance.Join(MainChannel);
				Game.Settings.Irc.Nickname = Instance.LocalUser.Nickname;
				Game.Settings.Save();
			};
			Instance.OnConnecting += () => AddHistory("Connecting");
			Instance.OnConnect += () => AddHistory("Connected");
			Instance.OnPart += l => AddHistory("{0} left{1}".F(l.Prefix.Nickname, l.Message != null ? ": " + l.Message : ""));
			Instance.OnJoin += l => AddHistory("{0} joined".F(l.Prefix.Nickname));
			Instance.OnQuit += l => AddHistory("{0} quit{1}".F(l.Prefix.Nickname, l.Message != null ? ": " + l.Message : ""));
			Instance.OnKick += l => AddHistory("{0} kicked {1}{2}".F(l.Prefix.Nickname, l.KickeeNickname, l.Message != null ? ": " + l.Message : ""));
			Instance.OnNicknameSet += l =>
			{
				AddHistory("{0} set their nickname to {1}".F(l.Prefix.Nickname, l.NewNickname));
				if (l.NewNickname == Instance.LocalUser.Nickname)
				{
					Instance.Nickname = l.NewNickname;
					Game.Settings.Irc.Nickname = l.NewNickname;
					Game.Settings.Save();
				}
			};
			Instance.OnTopicSet += l => AddHistory("{0} set the topic to {1}".F(l.Prefix.Nickname, l.Message));
			Instance.OnNumeric += l =>
			{
				if (l.Numeric == NumericCommand.RPL_TOPIC)
				{
					var topic = Instance.GetChannel(MainChannel).Topic;
					AddHistory("Topic is {0}".F(topic.Message));
				}
				else if (l.Numeric == NumericCommand.RPL_TOPICWHOTIME)
				{
					var topic = Instance.GetChannel(MainChannel).Topic;
					AddHistory("Topic set by {0} at {1}".F(topic.Author.Nickname, topic.Time.ToLocalTime()));
				}
				else if (l.Numeric == NumericCommand.RPL_NOTOPIC)
					AddHistory("No topic is set");
				else if (l.Numeric == NumericCommand.ERR_NICKNAMEINUSE)
					AddHistory("Nickname {0} is already in use".F(l.AltTarget));
				else if (l.Numeric == NumericCommand.ERR_ERRONEUSNICKNAME)
					AddHistory("Nickname {0} is erroneus".F(l.AltTarget));
			};
			Instance.OnDisconnect += () =>
			{
				if (Instance.ConnectionFailure != null)
				{
					AddHistory("Disconnected: {0}".F(Instance.ConnectionFailure.Message));
					if (s.ReconnectDelay >= 0)
					{
						AddHistory("Reconnecting in {0} seconds".F(s.ReconnectDelay / 1000));
						Instance.ConnectionState = IrcConnectionState.Reconnecting;
						Game.RunAfterDelay(s.ReconnectDelay, () =>
						{
							if (Instance.IsReconnecting)
								Instance.Connect(Instance.Hostname, Instance.Port, Instance.ConnectionTimeout, Instance.Nickname, Instance.Username, Instance.Realname);
						});
					}
				}
				else
					AddHistory("Disconnected");
			};
			Instance.OnLineRead += l =>
			{
				if (s.Debug)
					AddHistory(l.RawString);
			};
			Game.OnQuit += Instance.Disconnect;
		}

		static IrcClient()
		{
			Log.AddChannel("irc", "irc.log");
			InstanceInitialize();
		}

		public readonly ObservableCollection<string> History = new ObservableCollection<string>();

		IrcConnection connection;
		Thread thread;
		public IrcConnectionState ConnectionState { get; private set; }
		public IrcClientUser LocalUser { get; private set; }
		public Exception ConnectionFailure { get; private set; }

		public string Hostname { get; private set; }
		public int Port { get; private set; }
		public int ConnectionTimeout { get; private set; }
		public string Nickname { get; private set; }
		public string Username { get; private set; }
		public string Realname { get; private set; }

		public bool IsConnected
		{
			get { return ConnectionState == IrcConnectionState.Connected; }
		}

		public bool IsReconnecting
		{
			get { return ConnectionState == IrcConnectionState.Reconnecting; }
		}

		public IrcClient()
		{
			ConnectionState = IrcConnectionState.Disconnected;
		}

		public void Connect(string hostname, int port, int connectionTimeout, string nickname, string username, string realname)
		{
			ConnectionFailure = null;
			if (IsConnected)
				Disconnect();

			Hostname = hostname;
			Port = port;
			ConnectionTimeout = connectionTimeout;
			Nickname = nickname;
			Username = username;
			Realname = realname;

			thread = new Thread(() =>
			{
				try
				{
					ConnectionState = IrcConnectionState.Connecting;
					LocalUser = new IrcClientUser(this);
					connection = new IrcConnection();
					OnConnecting();
					connection.Connect(hostname, port, connectionTimeout);
					ConnectionState = IrcConnectionState.Connected;
					OnConnect();
					SetNickname(nickname);
					SetUser(username, realname);
					ProcessLines();
				}
				catch (Exception e)
				{
					Log.Write("irc", e.ToString());
					if (e is SocketException || e is IOException)
						ConnectionFailure = e;
				}
				finally
				{
					Disconnect();
				}
			}) { IsBackground = true };
			thread.Start();
		}

		public void WriteLine(string format, params object[] args)
		{
			try
			{
				connection.WriteLine(format, args);
			}
			catch (Exception e)
			{
				Log.Write("irc", e.ToString());
				if (e is SocketException || e is IOException)
					ConnectionFailure = e;
				Disconnect();
			}
		}

		public void Disconnect()
		{
			if (!IsConnected || IsReconnecting)
			{
				ConnectionState = IrcConnectionState.Disconnected;
				return;
			}

			ConnectionState = IrcConnectionState.Disconnecting;
			OnDisconnecting();
			connection.Close();
			ConnectionState = IrcConnectionState.Disconnected;
			OnDisconnect();
			LocalUser = null;
			connection = null;
		}

		void IDisposable.Dispose()
		{
			Disconnect();
		}

		void ProcessLines()
		{
			string line;
			while (IsConnected && (line = connection.ReadLine()) != null)
				ProcessLine(line);
		}

		void ProcessLine(string line)
		{
			if (string.IsNullOrEmpty(line))
				return;

			var l = new Line(this, line);
			OnLineRead(l);

			int numeric;
			if (int.TryParse(l.Command, out numeric))
			{
				var nl = new NumericLine(l, numeric);
				LocalUser.OnNumeric(nl);
				OnNumeric(nl);
				switch (nl.Numeric)
				{
					case NumericCommand.RPL_WELCOME:
						OnRegister(nl);
						break;
					case NumericCommand.RPL_ENDOFNAMES:
						OnSync(nl);
						break;
				}
			}
			else
			{
				switch (l.Command)
				{
					case "PING":
						Pong(l.Message);
						OnPing(l);
						break;
					case "PRIVMSG":
						if (IrcUtils.IsChannel(l.Target))
							OnPublicMessage(l);
						else
							OnPrivateMessage(l);
						break;
					case "NOTICE":
						if (IrcUtils.IsChannel(l.Target))
							OnPublicNotice(l);
						else
							OnPrivateNotice(l);
						break;
					case "JOIN":
						var jl = new JoinLine(l);
						LocalUser.OnJoin(jl);
						OnJoin(jl);
						break;
					case "PART":
						LocalUser.OnPart(l);
						OnPart(l);
						break;
					case "NICK":
						var nsl = new NicknameSetLine(l);
						LocalUser.OnNicknameSet(nsl);
						OnNicknameSet(nsl);
						break;
					case "QUIT":
						OnQuit(l);
						LocalUser.OnQuit(l);
						break;
					case "KICK":
						var kl = new KickLine(l);
						LocalUser.OnKick(kl);
						OnKick(kl);
						break;
					case "TOPIC":
						LocalUser.OnTopicSet(l);
						OnTopicSet(l);
						break;
				}
			}
		}

		public event Action<NumericLine> OnRegister = l => { };
		public event Action<NumericLine> OnSync = l => { };
		public event Action<Line> OnLineRead = _ => { };
		public event Action OnConnect = () => { };
		public event Action OnConnecting = () => { };
		public event Action OnDisconnect = () => { };
		public event Action OnDisconnecting = () => { };
		public event Action<Line> OnPublicMessage = _ => { };
		public event Action<Line> OnPublicNotice = _ => { };
		public event Action<Line> OnPrivateMessage = _ => { };
		public event Action<Line> OnPrivateNotice = _ => { };
		public event Action<JoinLine> OnJoin = _ => { };
		public event Action<Line> OnPart = _ => { };
		public event Action<NicknameSetLine> OnNicknameSet = _ => { };
		public event Action<Line> OnQuit = _ => { };
		public event Action<Line> OnPing = _ => { };
		public event Action<NumericLine> OnNumeric = _ => { };
		public event Action<KickLine> OnKick = _ => { };
		public event Action<Line> OnTopicSet = _ => { };

		public void SetNickname(string nickname) { WriteLine("NICK {0}", nickname); }
		public void SetUser(string username, string realname) { WriteLine("USER {0} 0 * :{1}", username, realname); }
		public void Join(string channel) { WriteLine("JOIN {0}", channel); }
		public void Part(string channel) { WriteLine("PART {0}", channel); }
		public void Message(string target, string message) { WriteLine("PRIVMSG {0} :{1}", target, message); }
		public void Notice(string target, string message) { WriteLine("NOTICE {0} :{1}", target, message); }
		public void Pong(string reply) { WriteLine("PONG :{0}", reply); }
		public void CtcpRequest(string target, string command, string request) { Message(target, IrcUtils.ToCtcp("{0} {1}".F(command, request))); }
		public void CtcpRespond(string target, string command, string response) { Notice(target, IrcUtils.ToCtcp("{0} {1}".F(command, response))); }
		public void Act(string target, string message) { Message(target, IrcUtils.ToAction(message)); }
		public void GetTopic(string channel) { WriteLine("TOPIC {0}", channel); }
		public void Quit(string message) { WriteLine("QUIT :{0}", message); }

		public Channel GetChannel(string channel)
		{
			if (!IsConnected)
				return null;
			Channel c;
			LocalUser.Channels.TryGetValue(channel, out c);
			return c;
		}
	}

	public enum IrcConnectionState
	{
		Disconnected,
		Connected,
		Disconnecting,
		Connecting,
		Reconnecting
	}
}
