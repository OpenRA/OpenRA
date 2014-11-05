#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Network;
using OpenRA.Server;

namespace OpenRA.Mods.RA.Server
{
	public class LobbySettingsNotification : ServerTrait, IClientJoined
	{
		public void ClientJoined(OpenRA.Server.Server server, Connection conn)
		{
			if (server.LobbyInfo.ClientWithIndex(conn.PlayerIndex).IsAdmin)
				return;

			var defaults = new Session.Global();
			FieldLoader.Load(defaults, Game.modData.Manifest.LobbyDefaults);

			if (server.LobbyInfo.GlobalSettings.FragileAlliances != defaults.FragileAlliances)
				server.SendOrderTo(conn, "Message", "Diplomacy Changes: {0}".F(server.LobbyInfo.GlobalSettings.FragileAlliances));

			if (server.LobbyInfo.GlobalSettings.AllowCheats != defaults.AllowCheats)
				server.SendOrderTo(conn, "Message", "Allow Cheats: {0}".F(server.LobbyInfo.GlobalSettings.AllowCheats));

			if (server.LobbyInfo.GlobalSettings.Shroud != defaults.Shroud)
				server.SendOrderTo(conn, "Message", "Shroud: {0}".F(server.LobbyInfo.GlobalSettings.Shroud));

			if (server.LobbyInfo.GlobalSettings.Fog != defaults.Fog)
				server.SendOrderTo(conn, "Message", "Fog of war: {0}".F(server.LobbyInfo.GlobalSettings.Fog));

			if (server.LobbyInfo.GlobalSettings.Crates != defaults.Crates)
				server.SendOrderTo(conn, "Message", "Crates Appear: {0}".F(server.LobbyInfo.GlobalSettings.Crates));

			if (server.LobbyInfo.GlobalSettings.AllyBuildRadius != defaults.AllyBuildRadius)
				server.SendOrderTo(conn, "Message", "Build off Ally ConYards: {0}".F(server.LobbyInfo.GlobalSettings.AllyBuildRadius));

			if (server.LobbyInfo.GlobalSettings.StartingUnitsClass != defaults.StartingUnitsClass)
			{
				var startUnitsInfo = server.Map.Rules.Actors["world"].Traits.WithInterface<MPStartUnitsInfo>();
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