#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using Eluant;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Player")]
	public class PlayerGlobal : ScriptGlobal
	{
		public PlayerGlobal(ScriptContext context)
			: base(context) { }

		[Desc("Returns the player with the specified internal name, or nil if a match is not found.")]
		public Player GetPlayer(string name)
		{
			return Context.World.Players.FirstOrDefault(p => p.InternalName == name);
		}

		[Desc("Returns a table of players filtered by the specified function.")]
		public Player[] GetPlayers(LuaFunction filter)
		{
			return FilteredObjects(Context.World.Players, filter).ToArray();
		}
	}
}
