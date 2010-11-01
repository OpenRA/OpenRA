#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Server
{
	public interface IServerExtension
	{
		/// <summary>
		/// Return true to use the built-in handling
		/// </summary>
		bool OnReadyUp(Connection conn, Session.Client client);
		void OnStartGame();
		/// <summary>
		/// Return true to use the built-in handling
		/// </summary>
		bool OnNickChange(Connection conn, Session.Client client, string newName);

		/// <summary>
		/// Return true to use the built-in handling
		/// </summary>
		bool OnRaceChange(Connection conn, Session.Client client, string newRace);

		/// <summary>
		/// Return true to use the built-in handling
		/// </summary>
		bool OnSlotChange(Connection conn, Session.Client client, Session.Slot slot, Map map);

		/// <summary>
		/// Return true to use the built-in handling
		/// </summary>
		bool OnTeamChange(Connection conn, Session.Client getClient, int team);

		/// <summary>
		/// Return true to use the built-in handling
		/// </summary>
		bool OnSpawnpointChange(Connection conn, Session.Client getClient, int spawnPoint);

		/// <summary>
		/// Return true to use the built-in handling
		/// </summary>
		bool OnColorChange(Connection conn, Session.Client getClient, Color fromArgb, Color color);

		/// <summary>
		/// Return true to use the built-in handling
		/// </summary>
		bool OnChat(Connection conn, string message, bool teamChat);

		void OnServerStart();
		void OnServerStop(bool forced);
		void OnLoadMap(Map map);

		/// <summary>
		/// Return false to drop the connection
		/// </summary>
		bool OnValidateConnection(bool gameStarted, Connection newConn);

		void OnLobbySync(Session lobbyInfo, bool gameStarted);
		/// <summary>
		/// Return true to use the built-in handling
		/// </summary>
		bool OnPingMasterServer(Session lobbyInfo, bool gameStarted);

		/// <summary>
		/// Return true to use the built-in handling
		/// </summary>
		bool OnIngameChat(Session.Client client, string message, bool teamChat);

		void OnIngameSetStance(Player player, Player stanceForPlayer, Stance newStance);

		void OnLobbyUp();
		void OnRejoinLobby(World world);
	}
}
