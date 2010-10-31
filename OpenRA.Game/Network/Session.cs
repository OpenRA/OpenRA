#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Network
{
	public class Session
	{
		public List<Client> Clients = new List<Client>();
		public List<Slot> Slots = new List<Slot>();
		public Global GlobalSettings = new Global();

		public Client ClientWithIndex( int clientID )
		{
			return Clients.SingleOrDefault( c => c.Index == clientID );
		}

		public Client ClientInSlot( Slot slot )
		{
			return Clients.SingleOrDefault( c => c.Slot == slot.Index );
		}

		public enum ClientState
		{
			NotReady,
			Ready
		}

		public class Client
		{
			public int Index;
			public Color Color1;
			public Color Color2;
			public string Country;
			public int SpawnPoint;
			public string Name;
			public ClientState State;
			public int Team;
			public int Slot;	//	which slot we're in, or -1 for `observer`.
		}

		public class Slot
		{
			public int Index;
			public string Bot;	// trait name of the bot to initialize in this slot, or null otherwise.
			public bool Closed;	// host has explicitly closed this slot.
			public string MapPlayer;	// playerReference to bind against.
			public bool Spectator = false; // Spectating or not
			// todo: more stuff?
		}

		public class Global
		{
			public string ServerName;
			public string Map;
			public string[] Mods = { "ra" };	// mod names
			public int OrderLatency = 3;
			public int RandomSeed = 0;
			public bool LockTeams = false;	// don't allow team changes after game start.
			public bool AllowCheats = false;
		}

		public Session( string[] mods )
		{
			this.GlobalSettings.Mods = mods.ToArray();
		}

		public string Serialize()
		{
			var clientData = new List<MiniYamlNode>();

			foreach( var client in Clients )
				clientData.Add( new MiniYamlNode( "Client@{0}".F( client.Index ), FieldSaver.Save( client ) ) );

			foreach( var slot in Slots )
				clientData.Add( new MiniYamlNode( "Slot@{0}".F( slot.Index ), FieldSaver.Save( slot ) ) );

			clientData.Add( new MiniYamlNode( "GlobalSettings", FieldSaver.Save( GlobalSettings ) ) );

			return clientData.WriteToString();
		}

		public static Session Deserialize(string data)
		{
			var session = new Session( Game.Settings.Game.Mods );

			var ys = MiniYaml.FromString(data);
			foreach (var y in ys)
			{
				var yy = y.Key.Split('@');

				switch( yy[0] )
				{
					case "GlobalSettings":
						FieldLoader.Load(session.GlobalSettings, y.Value);
						break;

					case "Client":
						session.Clients.Add(FieldLoader.Load<Session.Client>(y.Value));
						break;

					case "Slot":
						session.Slots.Add(FieldLoader.Load<Session.Slot>(y.Value ));
						break;
				}
			}

			return session;
		}
	}
}
