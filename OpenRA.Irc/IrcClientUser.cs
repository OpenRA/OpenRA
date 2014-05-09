#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Irc
{
	public class IrcClientUser : User
	{
		public readonly ObservableDictionary<string, Channel> Channels = new ObservableDictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);
		public readonly IrcClient Client;

		public IrcClientUser(IrcClient client)
		{
			Client = client;
		}

		public void OnNumeric(NumericLine line)
		{
			switch (line.Numeric)
			{
				case NumericCommand.RPL_WELCOME:
					new User(line.Message.Substring(line.Message.LastIndexOf(' ') + 1)).CopyTo(this);
					break;
				case NumericCommand.RPL_NAMREPLY:
					{
						var channel = line.GetChannel();
						var nicknames = line.Message.Replace("~", "").Replace("&", "").Replace("@", "").Replace("%", "").Replace("+", "").Split(' ');

						foreach (var nickname in nicknames.Where(n => !channel.Users.ContainsKey(n)))
							channel.Users.Add(nickname, new User { Nickname = nickname });
					}
					break;
				case NumericCommand.RPL_TOPIC:
					line.GetChannel().Topic.Message = line.Message;
					break;
				case NumericCommand.RPL_TOPICWHOTIME:
					{
						var topic = line.GetChannel().Topic;
						topic.Author = new User(line[4]);
						topic.Time = IrcUtils.DateTimeFromUnixTime(Exts.ParseIntegerInvariant(line[5]));
					}
					break;
				case NumericCommand.ERR_NICKNAMEINUSE:
					if (line.Target == "*") // no nickname set yet
						Client.SetNickname(Client.Nickname + new Random().Next(10000, 99999));
					break;
			}
		}

		public void OnJoin(Line line)
		{
			if (line.PrefixIsSelf())
				Channels.Add(line.Target, new Channel(line.Target));

			line.GetChannel().Users.Add(line.Prefix.Nickname, new User(line.Prefix));
		}

		public void OnPart(Line line)
		{
			line.GetChannel().Users.Remove(line.Prefix.Nickname);

			if (line.PrefixIsSelf())
				Channels.Remove(line.Target);
		}

		public void OnNicknameSet(NicknameSetLine line)
		{
			if (line.PrefixIsSelf())
				Nickname = line.NewNickname;

			foreach (var channel in Channels.Values.Where(c => c.Users.ContainsKey(line.Prefix.Nickname)))
			{
				var user = channel.Users[line.Prefix.Nickname];
				channel.Users.Remove(line.Prefix.Nickname);
				user.Nickname = line.NewNickname;
				channel.Users.Add(line.NewNickname, user);
			}
		}

		public void OnQuit(Line line)
		{
			foreach (var channel in Channels)
				channel.Value.Users.Remove(line.Prefix.Nickname);
		}

		public void OnKick(KickLine line)
		{
			line.GetChannel().Users.Remove(line.KickeeNickname);

			if (line.KickeeNickname.EqualsIC(Nickname))
				Channels.Remove(line.Target);
		}

		public void OnTopicSet(Line line)
		{
			var topic = line.GetChannel().Topic;
			topic.Message = line.Message;
			topic.Author = line.Prefix;
			topic.Time = DateTime.UtcNow;
		}
	}
}
