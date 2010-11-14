#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion
using System;
using OpenRA.Network;

namespace OpenRA.Server
{	
	// Returns true if order is handled 
	public interface IInterpretCommand { bool InterpretCommand(Server server, Connection conn, Session.Client client, string cmd); }
	public interface INotifySyncLobbyInfo { void LobbyInfoSynced(Server server); }
	public interface INotifyServerStart { void ServerStarted(Server server); }
	public interface INotifyServerShutdown { void ServerShutdown(Server server); }
	public interface IStartGame { void GameStarted(Server server); }
	public interface IClientJoined { void ClientJoined(Server server, Connection conn); }
	public interface ITick
	{
		void Tick(Server server);
		int TickTimeout { get; }
	}
	
	public abstract class ServerTrait {}
	
	public class DebugServerTrait : ServerTrait, IInterpretCommand, IStartGame, INotifySyncLobbyInfo, INotifyServerStart, INotifyServerShutdown
	{		
		public bool InterpretCommand(Server server, Connection conn, Session.Client client, string cmd)
		{
			Console.WriteLine("Server received command from player {1}: {0}",cmd, conn.PlayerIndex);
			return false;
		}
		
		public void GameStarted(Server server)
		{
			Console.WriteLine("GameStarted()");
		}
		
		public void LobbyInfoSynced(Server server)
		{
			Console.WriteLine("LobbyInfoSynced()");
		}
		
		public void ServerStarted(Server server)
		{
			Console.WriteLine("ServerStarted()");
		}
		
		public void ServerShutdown(Server server)
		{
			Console.WriteLine("ServerShutdown()");
		}
	}
}
