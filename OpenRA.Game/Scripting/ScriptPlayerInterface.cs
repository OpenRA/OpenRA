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

namespace OpenRA.Scripting
{
	public class ScriptPlayerInterface : ScriptObjectWrapper
	{
		readonly Player player;

		protected override string DuplicateKeyError(string memberName) { return $"Player '{player.PlayerName}' defines the command '{memberName}' on multiple traits"; }
		protected override string MemberNotFoundError(string memberName) { return $"Player '{player.PlayerName}' does not define a property '{memberName}'"; }

		public ScriptPlayerInterface(ScriptContext context, Player player)
			: base(context)
		{
			this.player = player;
			Bind(CreateObjects(context.PlayerCommands, new object[] { context, player }));
		}
	}
}
