﻿#region Copyright & License Information
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
	public class Topic
	{
		public string Message;
		public User Author;
		public DateTime Time;

		public Topic() { }

		public Topic(string message, User author, DateTime time)
		{
			Message = message;
			Author = author;
			Time = time;
		}
	}
}
