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
using OpenRA.Server;
using OpenRA.Traits;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class SkirmishLogic : ServerTrait, IClientJoined
	{
		public void ClientJoined(S server, Connection conn)
		{
			if (server.Type != ServerType.Skirmish)
				return;

			var slot = server.LobbyInfo.FirstEmptyBotSlot();
			var bot = server.Map.PlayerActorInfo.TraitInfos<IBotInfo>().Select(t => t.Type).FirstOrDefault();
			var botController = server.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
			if (slot != null && bot != null)
				server.InterpretCommand($"slot_bot {slot} {botController.Index} {bot}", conn);
		}
	}
}
