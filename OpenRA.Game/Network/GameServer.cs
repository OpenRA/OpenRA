#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Network
{
	public class GameServer
	{
		public readonly int Id = 0;
		public readonly string Name = null;
		public readonly string Address = null;
		public readonly int State = 0;
		public readonly int Players = 0;
		public readonly string Map = null;
		public readonly string[] Mods = { };
		public readonly int TTL = 0;

		public Dictionary<string, string> UsefulMods
		{
			get
			{
				return Mods
					.Where(v => v.Contains('@'))
					.ToDictionary(v => v.Split('@')[0], v => v.Split('@')[1]);
			}
		}
	}
}
