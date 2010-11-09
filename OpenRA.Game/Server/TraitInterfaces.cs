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
	public interface IInterpretCommand { bool InterpretCommand(Connection conn, Session.Client client, string cmd); }
	public interface INotifySyncLobbyInfo { void LobbyInfoSynced(); }
	public interface INotifyServerStart { void ServerStarted(); }
	public interface INotifyServerShutdown { void ServerShutdown(); }
	public interface IStartGame { void GameStarted(); }
	public interface IClientJoined { void ClientJoined(Connection conn); }
	public interface ITick
	{
		void Tick();
		int TickTimeout { get; }
	}
	
	public abstract class ServerTrait {}
	
	public class DebugServerTrait : ServerTrait, IInterpretCommand, IStartGame, INotifySyncLobbyInfo, INotifyServerStart, INotifyServerShutdown
	{		
		public bool InterpretCommand(Connection conn, Session.Client client, string cmd)
		{
			Console.WriteLine("Server received command from player {1}: {0}",cmd, conn.PlayerIndex);
			return false;
		}
		
		public void GameStarted()
		{
			Console.WriteLine("GameStarted()");
		}
		
		public void LobbyInfoSynced()
		{
			Console.WriteLine("LobbyInfoSynced()");
		}
		
		public void ServerStarted()
		{
			Console.WriteLine("ServerStarted()");
		}
		
		public void ServerShutdown()
		{
			Console.WriteLine("ServerShutdown()");
		}
	}
}
