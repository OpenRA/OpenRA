#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;

namespace OpenRA.Scripting
{
	public class ScriptPlayerInterface : ScriptObjectWrapper
	{
		readonly Player player;

		protected override string DuplicateKeyError(string memberName) { return "Player '{0}' defines the command '{1}' on multiple traits".F(player.PlayerName, memberName); }
		protected override string MemberNotFoundError(string memberName) { return "Player '{0}' does not define a property '{1}'".F(player.PlayerName, memberName); }

		public ScriptPlayerInterface(ScriptContext context, Player player)
			: base(context)
		{
			this.player = player;

			var args = new object[] { context, player };
			var objects = context.PlayerCommands.Select(cg =>
			{
				var groupCtor = cg.GetConstructor(new Type[] { typeof(ScriptContext), typeof(Player) });
				return groupCtor.Invoke(args);
			});

			Bind(objects);
		}
	}
}
