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
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Server
{
	public class LobbySettingsNotification : ServerTrait, IClientJoined
	{
		public void ClientJoined(OpenRA.Server.Server server, Connection conn)
		{
			lock (server.LobbyInfo)
			{
				var defaults = new Session.Global();
				LobbyCommands.LoadMapSettings(server, defaults, server.Map);

				var options = server.Map.PlayerActorInfo.TraitInfos<ILobbyOptions>()
					.Concat(server.Map.WorldActorInfo.TraitInfos<ILobbyOptions>())
					.SelectMany(t => t.LobbyOptions(server.Map))
					.ToDictionary(o => o.Id, o => o);

				foreach (var kv in server.LobbyInfo.GlobalSettings.LobbyOptions)
				{
					if (!defaults.LobbyOptions.TryGetValue(kv.Key, out var def) || kv.Value.Value != def.Value)
						if (options.TryGetValue(kv.Key, out var option))
							server.SendOrderTo(conn, "Message", option.Name + ": " + option.Values[kv.Value.Value]);
				}
			}
		}
	}
}
