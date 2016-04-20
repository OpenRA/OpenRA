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

using System.Linq;
using OpenRA.Mods.Common.Traits;
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
			LobbyCommands.LoadMapSettings(defaults, server.Map.Rules);

			var options = server.Map.Rules.Actors["player"].TraitInfos<ILobbyOptions>()
				.Concat(server.Map.Rules.Actors["world"].TraitInfos<ILobbyOptions>())
				.SelectMany(t => t.LobbyOptions(server.Map.Rules))
				.ToDictionary(o => o.Id, o => o);

			foreach (var kv in server.LobbyInfo.GlobalSettings.LobbyOptions)
			{
				Session.LobbyOptionState def;
				if (!defaults.LobbyOptions.TryGetValue(kv.Key, out def) || kv.Value.Value != def.Value)
					server.SendOrderTo(conn, "Message", options[kv.Key].Name + ": " + kv.Value.Value);
			}

			if (server.LobbyInfo.GlobalSettings.StartingUnitsClass != defaults.StartingUnitsClass)
			{
				var startUnitsInfo = server.Map.Rules.Actors["world"].TraitInfos<MPStartUnitsInfo>();
				var selectedClass = startUnitsInfo.Where(u => u.Class == server.LobbyInfo.GlobalSettings.StartingUnitsClass).Select(u => u.ClassName).FirstOrDefault();
				var className = selectedClass != null ? selectedClass : server.LobbyInfo.GlobalSettings.StartingUnitsClass;
				server.SendOrderTo(conn, "Message", "Starting Units: {0}".F(className));
			}

			if (server.LobbyInfo.GlobalSettings.StartingCash != defaults.StartingCash)
				server.SendOrderTo(conn, "Message", "Starting Cash: ${0}".F(server.LobbyInfo.GlobalSettings.StartingCash));

			if (server.LobbyInfo.GlobalSettings.TechLevel != defaults.TechLevel)
				server.SendOrderTo(conn, "Message", "Tech Level: {0}".F(server.LobbyInfo.GlobalSettings.TechLevel));
		}
	}
}