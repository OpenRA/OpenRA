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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Eluant;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Scripting
{
	[ScriptGlobal("Player")]
	public class PlayerGlobal : ScriptGlobal
	{
		public PlayerGlobal(ScriptContext context) : base(context) { }

		[Desc("Returns the player with the specified internal name, or nil if a match is not found.")]
		public Player GetPlayer(string name)
		{
			return context.World.Players.FirstOrDefault(p => p.InternalName == name);
		}

		[Desc("Returns a table of players filtered by the specified function.")]
		public LuaTable GetPlayers(LuaFunction filter)
		{
			var players = context.World.Players
				.Select(p => p.ToLuaValue(context))
				.Where(a =>
				{
					using (var f = filter.Call(a))
						return f.First().ToBoolean();
				});

			return players.ToLuaTable(context);
		}
	}
}
