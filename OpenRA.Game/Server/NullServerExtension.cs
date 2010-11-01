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
	public class NullServerExtension : IServerExtension
	{
		public virtual bool OnReadyUp(Connection conn, Session.Client client) { return true; }
		public virtual void OnStartGame() { }
		public virtual bool OnNickChange(Connection conn, Session.Client client, string newName) { return true; }
		public virtual bool OnRaceChange(Connection conn, Session.Client client, string newRace) { return true; }
		public virtual bool OnSlotChange(Connection conn, Session.Client client, Session.Slot slot, Map map) { return true; }
		public virtual bool OnTeamChange(Connection conn, Session.Client getClient, int team) { return true; }
		public virtual bool OnSpawnpointChange(Connection conn, Session.Client getClient, int spawnPoint) { return true; }
		public virtual bool OnColorChange(Connection conn, Session.Client getClient, Color fromArgb, Color color) { return true; }
		public virtual bool OnChat(Connection conn, string message, bool teamChat) { return true; }
		public virtual void OnServerStart() { }
		public virtual void OnServerStop(bool forced) { }
		// Good spot to manipulate number of spectators! ie set Server.MaxSpectators
		public virtual void OnLoadMap(Map map) { }
		public virtual bool OnValidateConnection(bool gameStarted, Connection newConn) { return true; }
		public virtual void OnLobbySync(Session lobbyInfo, bool gameStarted) { }
		public virtual bool OnPingMasterServer(Session lobbyInfo, bool gameStarted) { return true; }
		public virtual bool OnIngameChat(Session.Client client, string message, bool teamChat) { return true; }
		public virtual void OnIngameSetStance(Player player, Player stanceForPlayer, Stance newStance) { }
		public virtual void OnLobbyUp() { }
		public virtual void OnRejoinLobby(World world) { }
	}
}
