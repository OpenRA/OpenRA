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
using OpenRA.Primitives;

namespace OpenRA.Irc
{
	public class Channel
	{
		public readonly string Name;
		public readonly ObservableSortedDictionary<string, User> Users = new ObservableSortedDictionary<string, User>(StringComparer.OrdinalIgnoreCase);
		public Topic Topic = new Topic();

		public Channel(string name)
		{
			Name = name;
		}
	}
}
