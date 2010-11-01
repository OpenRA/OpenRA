using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA
{
	public interface IServerExtension
	{
		/// <summary>
		/// Return true to use the build-in handling
		/// </summary>
		bool OnReadyUp(Connection conn, Session.Client client);
		void OnStartGame();
		/// <summary>
		/// Return true to use the build-in handling
		/// </summary>
		bool OnNickChange(Connection conn, Session.Client client, string newName);

		/// <summary>
		/// Return true to use the build-in handling
		/// </summary>
		bool OnRaceChange(Connection conn, Session.Client client, string newRace);

		/// <summary>
		/// Return true to use the build-in handling
		/// </summary>
		bool OnSlotChange(Connection conn, Session.Client client, Session.Slot slot, Map map);

		/// <summary>
		/// Return true to use the build-in handling
		/// </summary>
		bool OnTeamChange(Connection conn, Session.Client getClient, int team);

		/// <summary>
		/// Return true to use the build-in handling
		/// </summary>
		bool OnSpawnpointChange(Connection conn, Session.Client getClient, int spawnPoint);

		/// <summary>
		/// Return true to use the build-in handling
		/// </summary>
		bool OnColorChange(Connection conn, Session.Client getClient, Color fromArgb, Color color);

		/// <summary>
		/// Return true to use the build-in handling
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
		/// Return true to use the build-in handling
		/// </summary>
		bool OnPingMasterServer(Session lobbyInfo, bool gameStarted);

		/// <summary>
		/// Return true to use the build-in handling
		/// </summary>
		bool OnIngameChat(Session.Client client, string message, bool teamChat);

		void OnIngameSetStance(Player player, Player stanceForPlayer, Stance newStance);

		void OnLobbyUp();
		void OnRejoinLobby(World world);
	}

	public class NullServerExtension : IServerExtension
	{
		public virtual bool OnReadyUp(Connection conn, Session.Client client)
		{
			return true;
		}

		public virtual void OnStartGame()
		{

		}

		public virtual bool OnNickChange(Connection conn, Session.Client client, string newName)
		{
			return true;
		}

		public virtual bool OnRaceChange(Connection conn, Session.Client client, string newRace)
		{
			return true;
		}

		public virtual bool OnSlotChange(Connection conn, Session.Client client, Session.Slot slot, Map map)
		{
			return true;
		}

		public virtual bool OnTeamChange(Connection conn, Session.Client getClient, int team)
		{
			return true;
		}

		public virtual bool OnSpawnpointChange(Connection conn, Session.Client getClient, int spawnPoint)
		{
			return true;
		}

		public virtual bool OnColorChange(Connection conn, Session.Client getClient, Color fromArgb, Color color)
		{
			return true;
		}

		public virtual bool OnChat(Connection conn, string message, bool teamChat)
		{
			return true;
		}

		public virtual void OnServerStart()
		{
		}

		public virtual void OnServerStop(bool forced)
		{

		}

		public virtual void OnLoadMap(Map map)
		{
			// Good spot to manipulate amount of spectators! ie set Server.MaxSpectators
		}

		public virtual bool OnValidateConnection(bool gameStarted, Connection newConn)
		{
			return true;
		}

		public virtual void OnLobbySync(Session lobbyInfo, bool gameStarted)
		{
			
		}

		public virtual bool OnPingMasterServer(Session lobbyInfo, bool gameStarted)
		{
			return true;
		}

		public virtual bool OnIngameChat(Session.Client client, string message, bool teamChat)
		{
			return true;
		}

		public virtual void OnIngameSetStance(Player player, Player stanceForPlayer, Stance newStance)
		{

		}

		public virtual void OnLobbyUp()
		{

		}

		public virtual void OnRejoinLobby(World world)
		{
			
		}
	}
}
