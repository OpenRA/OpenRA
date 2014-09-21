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

namespace OpenRA.Irc
{
	public static class IrcUtils
	{
		public static bool IsChannel(string s)
		{
			return !string.IsNullOrEmpty(s) && s[0] == '#';
		}

		public static bool IsNickname(string s)
		{
			return !string.IsNullOrEmpty(s) && (char.IsLetter(s[0]) || NicknameSpecialChars.Contains(s[0]))
				&& s.Substring(1).All(c => char.IsLetterOrDigit(c) || NicknameSpecialChars.Contains(c) || c == '-');
		}

		const string NicknameSpecialChars = @"[]\`_^{|}";

		public static DateTime DateTimeFromUnixTime(int seconds)
		{
			return new DateTime(1970, 1, 1).AddSeconds(seconds);
		}

		public static bool EqualsIC(this string a, string b)
		{
			return a.Equals(b, StringComparison.OrdinalIgnoreCase);
		}

		public static string FromCtcp(string message)
		{
			if (message.Length < 2 || !message.StartsWith("\x0001") || !message.EndsWith("\x0001"))
				return null;

			return message.Substring(1, message.Length - 2);
		}

		public static string ToCtcp(string message)
		{
			return "\x0001{0}\x0001".F(message);
		}

		public static string FromAction(string message)
		{
			if (!message.StartsWith("\x0001ACTION ") || !message.EndsWith("\x0001"))
				return null;
		
			return message.Substring(8, message.Length - 8 - 1);
		}

		public static string ToAction(string message)
		{
			return "\x0001ACTION {0}\x0001".F(message);
		}
	}
}
