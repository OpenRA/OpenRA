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

namespace OpenRA.Irc
{
	public class User
	{
		public string Nickname;
		public string Username;
		public string Hostname;

		public User() { }

		public User(User user)
		{
			Nickname = user.Nickname;
			Username = user.Username;
			Hostname = user.Hostname;
		}

		public void CopyTo(User user)
		{
			user.Nickname = Nickname;
			user.Username = Username;
			user.Hostname = Hostname;
		}

		public User(string prefix)
		{
			if (string.IsNullOrEmpty(prefix))
				throw new ArgumentException("prefix");

			var ex = prefix.IndexOf('!');
			var at = prefix.IndexOf('@');

			if (ex >= 0 && at >= 0 && at < ex)
				throw new ArgumentException("Bogus input string: @ before !");

			if (ex >= 0)
			{
				Nickname = prefix.Substring(0, ex);
				if (at >= 0)
				{
					Username = prefix.Substring(ex + 1, at - ex - 1);
					Hostname = prefix.Substring(at + 1);
				}
				else
					Username = prefix.Substring(ex + 1);
			}
			else
				Nickname = prefix;
		}

		public override string ToString()
		{
			var ret = "" + Nickname;
			if (Username != null)
				ret += "!" + Username;
			if (Hostname != null)
				ret += "@" + Hostname;
			return ret;
		}
	}
}
