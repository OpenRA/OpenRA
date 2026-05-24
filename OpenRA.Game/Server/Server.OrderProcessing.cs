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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Server
{
	public sealed partial class Server
	{
		readonly Dictionary<int, byte[]> syncForFrame = [];
		int lastDefeatStateFrame;
		ulong lastDefeatState;

		static byte[] CreateFrame(int client, int frame, byte[] data)
		{
			var ms = new MemoryStream(data.Length + 12);
			ms.Write(data.Length + 4);
			ms.Write(client);
			ms.Write(frame);
			ms.Write(data);
			return ms.GetBuffer();
		}

		static byte[] CreateAckFrame(int frame, byte count)
		{
			var ms = new MemoryStream(14);
			ms.Write(6);
			ms.Write(0);
			ms.Write(frame);
			ms.WriteByte((byte)OrderType.Ack);
			ms.WriteByte(count);
			return ms.GetBuffer();
		}

		static byte[] CreateTickScaleFrame(float scale)
		{
			var ms = new MemoryStream(17);
			ms.Write(9);
			ms.Write(0);
			ms.Write(0);
			ms.WriteByte((byte)OrderType.TickScale);
			ms.Write(scale);
			return ms.GetBuffer();
		}

		void DispatchOrdersToClient(Connection c, int client, int frame, byte[] data)
		{
			DispatchFrameToClient(c, client, CreateFrame(client, frame, data));
		}

		void DispatchFrameToClient(Connection c, int client, byte[] frameData)
		{
			if (!c.TrySendData(frameData))
			{
				DropClient(c);
				Log.Write("server", $"Dropping client {client.ToString(CultureInfo.InvariantCulture)} because dispatching orders failed!");
			}
		}

		bool AnyUndefinedWinStates()
		{
			var lastTeam = -1;
			var remainingPlayers = gameInfo.Players.Where(p => p.Outcome == WinState.Undefined);
			foreach (var player in remainingPlayers)
			{
				if (lastTeam >= 0 && (player.Team != lastTeam || player.Team == 0))
					return true;

				lastTeam = player.Team;
			}

			return false;
		}

		void SetPlayerDefeat(int playerIndex)
		{
			var defeatedPlayer = worldPlayers[playerIndex];
			if (defeatedPlayer == null || defeatedPlayer.Outcome != WinState.Undefined)
				return;

			defeatedPlayer.Outcome = WinState.Lost;
			defeatedPlayer.OutcomeTimestampUtc = DateTime.UtcNow;

			// Set remaining players as winners if only one side remains
			if (!AnyUndefinedWinStates())
			{
				var now = DateTime.UtcNow;
				var remainingPlayers = gameInfo.Players.Where(p => p.Outcome == WinState.Undefined);
				foreach (var winner in remainingPlayers)
				{
					winner.Outcome = WinState.Won;
					winner.OutcomeTimestampUtc = now;
				}
			}
		}

		void OutOfSync(int frame)
		{
			Log.Write("server", $"Out of sync detected at frame {frame}, cancel replay recording");

			// Make sure the written file is not valid
			// TODO: storing a serverside replay on desync would be extremely useful
			if (recorder != null)
			{
				recorder.Metadata = null;

				recorder.Dispose();
			}

			// Stop the recording
			recorder = null;
		}

		void HandleSyncOrder(int frame, byte[] packet)
		{
			if (syncForFrame.TryGetValue(frame, out var existingSync))
			{
				if (packet.Length != existingSync.Length)
				{
					OutOfSync(frame);
					return;
				}

				for (var i = 0; i < packet.Length; i++)
				{
					if (packet[i] != existingSync[i])
					{
						OutOfSync(frame);
						return;
					}
				}
			}
			else
			{
				// Update player losses based on the new defeat state.
				// Do this once for the first player, the check above
				// guarantees a desync if any other player disagrees.
				var playerDefeatState = BitConverter.ToUInt64(packet, 1 + 4);
				if (frame > lastDefeatStateFrame && lastDefeatState != playerDefeatState)
				{
					var newDefeats = playerDefeatState & ~lastDefeatState;
					for (var i = 0; i < worldPlayers.Count; i++)
						if ((newDefeats & (1UL << i)) != 0)
							SetPlayerDefeat(i);

					lastDefeatState = playerDefeatState;
					lastDefeatStateFrame = frame;
				}

				syncForFrame.Add(frame, packet);
			}
		}

		public void DispatchOrdersToClients(Connection conn, int frame, byte[] data)
		{
			var from = conn.PlayerIndex;
			var frameData = CreateFrame(from, frame, data);
			foreach (var c in Conns.ToList())
				if (c != conn && c.Validated)
					DispatchFrameToClient(c, from, frameData);

			RecordOrder(frame, data, from);
		}

		void RecordOrder(int frame, byte[] data, int from)
		{
			recorder?.ReceiveFrame(from, frame, data);

			if (data.Length > 0 && data[0] == (byte)OrderType.SyncHash)
			{
				if (data.Length == Order.SyncHashOrderLength)
					HandleSyncOrder(frame, data);
				else
					Log.Write("server", $"Dropped sync order with length {data.Length} from client {from}. Expected length {Order.SyncHashOrderLength}.");
			}
		}

		public void DispatchServerOrdersToClients(Order order)
		{
			DispatchServerOrdersToClients(order.Serialize());
		}

		public void DispatchServerOrdersToClients(byte[] data, int frame = 0)
		{
			const int From = 0;
			var frameData = CreateFrame(From, frame, data);
			foreach (var c in Conns.ToList())
				if (c.Validated)
					DispatchFrameToClient(c, From, frameData);

			RecordOrder(frame, data, From);
		}

		public void DispatchServerOrdersToClients(ReadOnlySpan<Connection> conns, byte[] data, int frame = 0)
		{
			const int From = 0;
			var frameData = CreateFrame(From, frame, data);
			foreach (var c in conns)
				if (c.Validated)
					DispatchFrameToClient(c, From, frameData);

			RecordOrder(frame, data, From);
		}

		public void ReceiveOrders(Connection conn, int frame, byte[] data)
		{
			// Make sure we don't accidentally forward on orders from clients who we have just dropped
			if (!Conns.Contains(conn))
				return;

			if (frame == 0)
				InterpretServerOrders(conn, data);
			else
			{
				// Non-immediate orders must be projected into the future so that all players can
				// apply them on the same world tick. We can do this directly when forwarding the
				// packet on to other clients, but sending the same data back to the client that
				// sent it just to update the frame number would be wasteful. We instead send them
				// a separate Ack packet that tells them to apply the order from a locally stored queue.
				// TODO: Replace static latency with a dynamic order buffering system
				if (data.Length == 0 || data[0] != (byte)OrderType.SyncHash)
				{
					frame += OrderLatency;
					DispatchFrameToClient(conn, conn.PlayerIndex, CreateAckFrame(frame, 1));

					orderBuffer?.AddOrderTimestamp(conn.PlayerIndex);

					// Track the last frame for each client so the disconnect handling can write
					// an EndOfOrders marker with the correct frame number.
					// TODO: This should be handled by the order buffering system too
					conn.LastOrdersFrame = frame;
				}

				DispatchOrdersToClients(conn, frame, data);
			}

			GameSave?.DispatchOrders(conn, frame, data);
		}

		void InterpretServerOrders(Connection conn, byte[] data)
		{
			var ms = new MemoryStream(data);
			var br = new BinaryReader(ms);

			try
			{
				while (ms.Position < ms.Length)
				{
					var o = Order.Deserialize(null, br);
					if (o != null)
						InterpretServerOrder(conn, o);
				}
			}
			catch (EndOfStreamException) { }
			catch (NotImplementedException) { }
		}

		void InterpretServerOrder(Connection conn, Order o)
		{
			lock (LobbyInfo)
			{
				// Only accept handshake responses from unvalidated clients
				// Anything else may be an attempt to exploit the server
				if (!conn.Validated)
				{
					if (o.OrderString == "HandshakeResponse")
						ValidateClient(conn, o.TargetString, o.OrderString);
					else
					{
						Log.Write("server", $"Rejected connection from {conn.EndPoint}; Order `{o.OrderString}` is not a `HandshakeResponse`.");
						DropClient(conn);
					}

					return;
				}

				switch (o.OrderString)
				{
					case "Command":
					{
						if (!InterpretCommand(o.TargetString, conn))
						{
							Log.Write("server", $"Unknown server command: {o.TargetString}");
							SendFluentMessageTo(conn, UnknownServerCommand, ["command", o.TargetString]);
						}

						break;
					}

					case "Chat":
					{
						if (!IsMultiplayer || !playerMessageTracker.IsPlayerAtFloodLimit(conn))
							DispatchOrdersToClients(conn, 0, o.Serialize());

						break;
					}

					case "GameSaveTraitData":
					{
						if (GameSave != null)
						{
							var data = MiniYaml.FromString(o.TargetString, o.OrderString).First();
							GameSave.AddTraitData(OpenRA.Exts.ParseInt32Invariant(data.Key), data.Value);
						}

						break;
					}

					case "CreateGameSave":
					{
						if (GameSave != null)
						{
							// Sanitize potentially malicious input
							var filename = o.TargetString;
							var invalidIndex = -1;
							var invalidChars = Path.GetInvalidFileNameChars();
							while ((invalidIndex = filename.IndexOfAny(invalidChars)) != -1)
								filename = filename.Remove(invalidIndex, 1);

							var baseSavePath = Path.Combine(
								Platform.SupportDir,
								"Saves",
								ModData.Manifest.Id,
								ModData.Manifest.Metadata.Version);

							if (!Directory.Exists(baseSavePath))
								Directory.CreateDirectory(baseSavePath);

							GameSave.Save(Path.Combine(baseSavePath, filename));
							DispatchServerOrdersToClients(Order.FromTargetString("GameSaved", filename, true, o.ExtraData));
						}

						break;
					}

					case "LoadGameSave":
					{
						if (Type == ServerType.Dedicated || State >= ServerState.GameStarted)
							break;

						// Sanitize potentially malicious input
						var filename = o.TargetString;
						var invalidIndex = -1;
						var invalidChars = Path.GetInvalidFileNameChars();
						while ((invalidIndex = filename.IndexOfAny(invalidChars)) != -1)
							filename = filename.Remove(invalidIndex, 1);

						var savePath = Path.Combine(
							Platform.SupportDir,
							"Saves",
							ModData.Manifest.Id,
							ModData.Manifest.Metadata.Version,
							filename);

						GameSave = new GameSave(savePath);
						LobbyInfo.GlobalSettings = GameSave.GlobalSettings;
						LobbyInfo.Slots = GameSave.Slots;

						// Reassign clients to slots
						//  - Bot ordering is preserved
						//  - Humans are assigned on a first-come-first-serve basis
						//  - Leftover humans become spectators

						// Start by removing all bots and assigning all players as spectators
						foreach (var c in LobbyInfo.Clients)
						{
							if (c.Bot != null)
								LobbyInfo.Clients.Remove(c);
							else
								c.Slot = null;
						}

						// Rebuild/remap the saved client state
						// TODO: Multiplayer saves should leave all humans as spectators so they can manually pick slots
						var adminClientIndex = LobbyInfo.Clients.First(c => c.IsAdmin).Index;
						foreach (var kv in GameSave.SlotClients)
						{
							if (kv.Value.Bot != null)
							{
								var bot = new Session.Client()
								{
									Index = ChooseFreePlayerIndex(),
									State = Session.ClientState.NotReady,
									BotControllerClientIndex = adminClientIndex
								};

								kv.Value.ApplyTo(bot);
								LobbyInfo.Clients.Add(bot);
							}
							else
							{
								// This will throw if the server doesn't have enough human clients to fill all player slots
								// See TODO above - this isn't a problem in practice because MP saves won't use this
								var client = LobbyInfo.Clients.First(c => c.Slot == null);
								kv.Value.ApplyTo(client);
							}
						}

						SyncLobbyInfo();
						SyncLobbyClients();

						break;
					}

					case "GenerateMap":
					{
						if (!GetClient(conn).IsAdmin || State >= ServerState.GameStarted)
							break;

						if (!LobbyInfo.GlobalSettings.EnableMapGeneration)
							break;

						try
						{
							var yaml = new MiniYaml(o.OrderString, MiniYaml.FromString(o.TargetString, o.OrderString));
							var args = FieldLoader.Load<MapGenerationArgs>(yaml);
							var preview = ModData.MapCache[args.Uid];
							if (preview.Status != MapStatus.Available && preview.Class != MapClassification.Generated)
								preview.UpdateFromGenerationArgs(args);

							GeneratedMapData = o.TargetString;
							DispatchServerOrdersToClients(Order.FromTargetString("GenerateMap", o.TargetString, true));
						}
						catch (Exception e)
						{
							Console.WriteLine(e);
							throw;
						}

						break;
					}
				}
			}
		}
	}
}
