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
using System.Linq;

namespace OpenRA.Irc
{
	public class Line
	{
		public readonly IrcClient Client;
		public readonly string RawString;
		public readonly string[] RawStringParts;
		public readonly User Prefix;
		public readonly string Command;
		public string Target { get; protected set; }
		public string Message { get; protected set; }

		public Line(Line line)
		{
			Client = line.Client;
			RawString = line.RawString;
			RawStringParts = line.RawStringParts;
			Prefix = line.Prefix;
			Command = line.Command;
			Target = line.Target;
			Message = line.Message;
		}

		public Line(IrcClient client, string line)
		{
			RawString = line;
			RawStringParts = line.Split(' ');
			Client = client;

			if (line[0] == ':')
			{
				line = line.Substring(1);
				var prefixDelim = line.Split(new[] { ' ' }, 2);
				Prefix = new User(prefixDelim[0]);

				if (prefixDelim.Length > 1)
				{
					var messageDelim = prefixDelim[1].Split(new[] { ':' }, 2);

					var args = messageDelim[0].Trim().Split(' ');

					Command = args[0];
					if (args.Length > 1)
						Target = args[1];

					if (messageDelim.Length > 1)
						Message = messageDelim[1];
				}
			}
			else
			{
				var messageDelim = line.Split(new[] { ':' }, 2);

				var args = messageDelim[0].Trim().Split(' ');

				Command = args[0];
				if (args.Length > 1)
					Target = args[1];

				if (messageDelim.Length > 1)
					Message = messageDelim[1];
			}
		}

		public virtual Channel GetChannel()
		{
			return Client.GetChannel(Target);
		}

		public string this[int index]
		{
			get { return RawStringParts[index]; }
		}

		public bool PrefixIsSelf()
		{
			return Client.LocalUser != null && Prefix.Nickname.EqualsIC(Client.LocalUser.Nickname);
		}

		public bool TargetIsSelf()
		{
			return Target != null && Target.EqualsIC(Client.LocalUser.Nickname);
		}
	}

	public class NicknameSetLine : Line
	{
		public readonly string NewNickname;

		public NicknameSetLine(Line line)
			: base(line)
		{
			NewNickname = Message;
		}
	}

	public class NumericLine : Line
	{
		public readonly NumericCommand Numeric;
		public readonly string AltTarget;

		public override Channel GetChannel()
		{
			if (IrcUtils.IsChannel(AltTarget))
				return Client.GetChannel(AltTarget);
			return Client.GetChannel(Target);
		}

		public NumericLine(Line line, int numeric)
			: base(line)
		{
			if (!IrcUtils.IsChannel(Target))
			{
				var numericParts = line.RawStringParts.Skip(1).TakeWhile(p => !p.StartsWith(":", StringComparison.Ordinal));
				AltTarget = numericParts.LastOrDefault(IrcUtils.IsChannel);
				if (AltTarget == null)
					AltTarget = numericParts.LastOrDefault();
			}
			Numeric = (NumericCommand)numeric;
		}
	}

	public class JoinLine : Line // for compatibility with certain IRCds
	{
		public JoinLine(Line line)
			: base(line)
		{
			if (Message != null) // don't overwrite the target if it was already set properly by the IRCd
				Target = Message;
		}
	}

	public class KickLine : Line
	{
		public readonly string KickeeNickname;

		public KickLine(Line line)
			: base(line)
		{
			KickeeNickname = this[3];
		}
	}
}
