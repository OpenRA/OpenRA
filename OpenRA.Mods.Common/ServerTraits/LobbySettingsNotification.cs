#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Server
{
	public class LobbySettingsNotification : ServerTrait, IClientJoined
	{
		public void ClientJoined(OpenRA.Server.Server server, Connection conn)
		{
			if (server.LobbyInfo.ClientWithIndex(conn.PlayerIndex).IsAdmin)
				return;

			var defaults = new Session.Global();
			LobbyCommands.LoadMapSettings(server, defaults, server.Map.Rules);

			var options = server.Map.Rules.Actors["player"].TraitInfos<ILobbyOptions>()
				.Concat(server.Map.Rules.Actors["world"].TraitInfos<ILobbyOptions>())
				.SelectMany(t => t.LobbyOptions(server.Map.Rules));

			var optionNames = new Dictionary<string, string>();
			foreach (var o in options)
				optionNames[o.Id] = o.Name;

			foreach (var kv in server.LobbyInfo.GlobalSettings.LobbyOptions)
			{
				Session.LobbyOptionState def;
				string optionName;
				if (!defaults.LobbyOptions.TryGetValue(kv.Key, out def) || kv.Value.Value != def.Value)
					if (optionNames.TryGetValue(kv.Key, out optionName))
						server.SendOrderTo(conn, "Message", optionName + ": " + kv.Value.Value);
			}
		}
	}
}
