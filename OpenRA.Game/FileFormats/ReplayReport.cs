#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.Network;

namespace OpenRA.FileFormats
{
	public class ReplayReport
	{
		static int networkTickDuration = 120; // Engine constant (ms)

		public static string Read(ReplayMetadata metadata)
		{
			if (metadata == null || metadata.FilePath == null)
				return null;

			var text = new StringBuilder();
			var session = new Session();

			var gameInfo = metadata.GameInfo;
			var playersByTeam = gameInfo.Players.GroupBy(p => p.Team).OrderBy(g => g.Key);

			var packetClientIndex = 0;
			var gameStarted = false;
			var time = "";

			var orderHandler = new Dictionary<string, Action<string>>
			{
				{ "SyncInfo", s => { session = Session.Deserialize(s); } },
				{ "SyncLobbyClients", s =>
					{
						session.Clients = new List<Session.Client>();
						var nodes = MiniYaml.FromString(s);
						foreach (var node in nodes)
						{
							if (!node.Key.StartsWith("Client@"))
								continue;

							var nodeClient = Session.Client.Deserialize(node.Value);
							session.Clients.Add(nodeClient);
						}
					}
				},
				{ "StartGame", s =>
					{
						text.AppendLine();
						text.AppendLine("#### Game Started ####");
						text.AppendLine();
						text.AppendLine("Map Title:      " + gameInfo.MapTitle);
						text.AppendLine("Map UID:        " + gameInfo.MapUid);
						text.AppendLine("Start Time UTC: " + gameInfo.StartTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"));
						text.AppendLine();

						foreach (var team in playersByTeam)
						{
							var teamLabel = team.Key == 0 ? "No Team:" : string.Format("Team {0}:", team.Key);
							text.AppendLine(teamLabel);
							text.AppendLine("--------");
							foreach (var player in team)
							{
								text.AppendLine(player.Name);
								var client = session.ClientWithIndex(player.ClientIndex);
								if (client.IsAdmin)
									text.AppendLine("\t[Admin]");

								if (player.IsBot)
									text.AppendLine("\t[Computer]");

								var factionRnd = player.IsRandomFaction ? " (Random)" : "";
								text.AppendLine("\tFaction:     " + player.FactionName + factionRnd);
								var spawnRnd = player.IsRandomSpawnPoint ? " (Random)" : "";
								text.AppendLine("\tSpawn point: " + player.SpawnPoint + spawnRnd);
								if (player.IsHuman && !gameInfo.IsSinglePlayer)
									AddNetworkInfo(session, client, text);

								text.AppendLine();
							}
						}

						var spectators = session.Clients.Where(c => c.IsObserver);
						if (spectators.Any())
						{
							text.AppendLine("Spectators:");
							text.AppendLine("-----------");
							foreach (var spec in spectators)
							{
								text.AppendLine(spec.Name);
								if (spec.IsAdmin)
									text.AppendLine("\t[Admin]");

								if (!gameInfo.IsSinglePlayer)
									AddNetworkInfo(session, spec, text);

								text.AppendLine();
							}
						}

						AddSettingsDisplay(session.GlobalSettings, text);
						text.AppendLine();
						gameStarted = true;
					}
				},
				{ "Chat", s =>
					{
						var client = session.ClientWithIndex(packetClientIndex);
						var specTag = client.IsObserver ? " (Spectator): " : ": ";
						text.AppendLine(time + client.Name + specTag + s);
					}
				},
				{ "TeamChat", s =>
					{
						text.AppendLine(time + session.ClientWithIndex(packetClientIndex).Name + " (Team): " + s);
					}
				},
				{ "Message", s =>
					{
						foreach (var line in s.Split('\n'))
							text.AppendLine(time + "Server: " + line);
					}
				}
			};

			try
			{
				using (var fs = File.OpenRead(metadata.FilePath))
				{
					var networkTickCount = 0;

					while (fs.Position < metadata.MetaStartMarkerPosition)
					{
						packetClientIndex = fs.ReadInt32();
						var packetLen = fs.ReadInt32();
						var packet = fs.ReadBytes(packetLen);

						if (packet.Length == 5 && packet[4] == 0xBF)
							continue; // disconnect

						if (packet.Length >= 5 && packet[4] == 0x65)
							continue; // sync

						var frame = BitConverter.ToInt32(packet, 0);
						networkTickCount = Math.Max(networkTickCount, frame);
						var orders = packet.ToOrderList(null);

						// Time stamps are in game time.
						if (gameStarted)
							time = new TimeSpan(0, 0, networkTickCount * networkTickDuration / 1000) + " ";

						foreach (var o in orders)
						{
							if (!orderHandler.ContainsKey(o.OrderString))
								continue;

							orderHandler[o.OrderString](o.TargetString);
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (ex is ArgumentException || ex is NotSupportedException || ex is IOException || ex is UnauthorizedAccessException)
				{
					Log.Write("debug", ex.ToString());
					return null;
				}

				throw;
			}

			text.AppendLine();
			text.AppendLine("#### Game Ended ####");
			text.AppendLine();
			text.AppendLine("Results:");

			foreach (var team in playersByTeam)
			{
				text.AppendLine();
				var teamLabel = team.Key == 0 ? "No Team:" : string.Format("Team {0}:", team.Key);
				text.AppendLine(teamLabel);
				text.AppendLine("--------");
				foreach (var player in team)
				{
					var timeDiff = "";
					if (player.Outcome != WinState.Undefined)
						timeDiff = "  " + player.OutcomeTimestampUtc.Subtract(gameInfo.StartTimeUtc);

					text.AppendLine(string.Format("{0,-16}  {1,-9}{2}", player.Name, player.Outcome, timeDiff));
				}
			}

			text.AppendLine();
			text.AppendLine("Game Duration: " + gameInfo.Duration);
			text.AppendLine("End Time UTC:  " + gameInfo.EndTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"));

			var header = new StringBuilder();

			var title = gameInfo.IsSinglePlayer ? "Single player" : "Server name: " + session.GlobalSettings.ServerName;
			header.AppendLine(title);
			header.AppendLine("Mod:         " + gameInfo.Mod);
			header.AppendLine("Version:     " + gameInfo.Version);
			header.AppendLine();

			return header.ToString() + text.ToString();
		}

		static void AddNetworkInfo(Session session, Session.Client client, StringBuilder text)
		{
			text.AppendLine("\tLatency:     " + session.PingFromClient(client).Latency + " ms");
			text.AppendLine("\tIP Address:  " + client.IpAddress);
			text.AppendLine("\tLocation:    " + GeoIP.LookupCountry(client.IpAddress));
		}

		static void AddSettingsDisplay(Session.Global settings, StringBuilder text)
		{
			var setDisp = new List<string>();
			var defaultSettings = new Session.Global();

			setDisp.Add("Default game settings");

			if (settings.Shroud != defaultSettings.Shroud)
				setDisp.Add("Shroud: " + settings.Shroud);

			if (settings.ShortGame != defaultSettings.ShortGame)
				setDisp.Add("Short Game: " + settings.ShortGame);

			if (settings.AllyBuildRadius != defaultSettings.AllyBuildRadius)
				setDisp.Add("Build off Allies' ConYards: " + settings.AllyBuildRadius);

			if (settings.Fog != defaultSettings.Fog)
				setDisp.Add("Fog of War: " + settings.Fog);

			if (settings.Crates != defaultSettings.Crates)
				setDisp.Add("Crates: " + settings.Crates);

			if (settings.FragileAlliances != defaultSettings.FragileAlliances)
				setDisp.Add("Diplomacy Changes: " + settings.FragileAlliances);

			if (settings.AllowCheats != defaultSettings.AllowCheats)
				setDisp.Add("Debug Menu: " + settings.AllowCheats);

			if (settings.StartingCash != defaultSettings.StartingCash)
				setDisp.Add("Starting Cash: " + settings.StartingCash);

			if (settings.StartingUnitsClass != defaultSettings.StartingUnitsClass)
				setDisp.Add("Starting Units: " + settings.StartingUnitsClass);

			if (settings.TechLevel != defaultSettings.TechLevel)
				setDisp.Add("Tech Level: " + settings.TechLevel);

			if (setDisp.Count > 1)
				setDisp[0] = "Custom game settings:";

			foreach (var line in setDisp)
				text.AppendLine(line);
		}
	}
}
