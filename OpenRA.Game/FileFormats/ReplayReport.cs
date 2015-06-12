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

			var gameInfo = metadata.GameInfo;
			var playersByTeam = gameInfo.Players.GroupBy(p => p.Team).OrderBy(g => g.Key);
			var session = new Session();

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
						text.AppendLine("Map Title: " + gameInfo.MapTitle);
						text.AppendLine("Map UID: " + gameInfo.MapUid);
						text.AppendLine("Start Time UTC: " + gameInfo.StartTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"));
						text.AppendLine();

						GeoIP.Initialize();
						foreach (var team in playersByTeam)
						{
							var teamLabel = team.Key == 0 ? "No Team:" : "Team " + team.Key + ":";
							text.AppendLine(teamLabel);
							text.AppendLine(new string('-', teamLabel.Length));
							foreach (var player in team)
							{
								text.AppendLine(player.Name);
								var client = session.ClientWithIndex(player.ClientIndex);
								if (client.IsAdmin && !gameInfo.IsSinglePlayer)
									text.AppendLine("\t[Admin]");

								text.AppendLine("\tHuman: " + player.IsHuman);
								text.AppendLine("\tFaction: " + player.FactionName);
								text.AppendLine("\tRandom faction: " + player.IsRandomFaction);
								text.AppendLine("\tSpawn point: " + player.SpawnPoint);
								text.AppendLine("\tRandom spawn point: " + player.IsRandomSpawnPoint);

								if (player.IsHuman && !gameInfo.IsSinglePlayer)
									foreach (var line in NetworkInfo(session, client))
										text.AppendLine("\t" + line);

								text.AppendLine();
							}
						}

						var spectators = session.Clients.Where(c => c.IsObserver);
						if (spectators.Any())
						{
							var specLabel = "Spectators:";
							text.AppendLine(specLabel);
							text.AppendLine(new string('-', specLabel.Length));
							foreach (var spec in spectators)
							{
								text.AppendLine(spec.Name);
								if (!gameInfo.IsSinglePlayer)
								{
									if (spec.IsAdmin)
										text.AppendLine("\t[Admin]");

									foreach (var line in NetworkInfo(session, spec))
										text.AppendLine("\t" + line);
								}

								text.AppendLine();
							}
						}

						foreach (var line in SettingsDisplay(session.GlobalSettings))
							text.AppendLine(line);

						text.AppendLine();
						gameStarted = true;
					}
				},
				{ "Chat", s =>
					{
						if (gameStarted)
							text.Append(time);

						var client = session.ClientWithIndex(packetClientIndex);
						text.Append(client.Name);
						text.Append(client.IsObserver ? " (Spectator): " : ": ");
						text.AppendLine(s);
					}
				},
				{ "TeamChat", s =>
					{
						if (gameStarted)
							text.Append(time);

						text.AppendLine(session.ClientWithIndex(packetClientIndex).Name + " (Team): " + s);
					}
				},
				{ "Message", s =>
					{
						foreach (var line in s.Split('\n'))
						{
							if (gameStarted)
								text.Append(time);

							text.AppendLine("Server: " + line);
						}
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
				// OutOfMemoryException can be thrown if the replay file is corrupt.
				if (ex is ArgumentException || ex is NotSupportedException || ex is IOException
					|| ex is UnauthorizedAccessException || ex is OutOfMemoryException)
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
				var teamLabel = team.Key == 0 ? "No Team:" : "Team " + team.Key + ":";
				text.AppendLine(teamLabel);
				text.AppendLine(new string('-', teamLabel.Length));
				foreach (var player in team)
					text.AppendLine(player.Name + " - " + player.Outcome);
			}

			text.AppendLine();
			text.AppendLine("Game Duration: " + gameInfo.Duration);
			text.AppendLine("End Time UTC: " + gameInfo.EndTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"));

			var header = new StringBuilder();

			if (gameInfo.IsSinglePlayer)
				header.AppendLine("Single player");
			else
				header.AppendLine("Server name: " + session.GlobalSettings.ServerName);

			header.AppendLine("Mod: " + gameInfo.Mod);
			header.AppendLine("Version: " + gameInfo.Version);
			header.AppendLine();

			return header.ToString() + text.ToString();
		}

		static string[] NetworkInfo(Session session, Session.Client client)
		{
			var netInf = new string[3];
			var ping = session.PingFromClient(client);

			netInf[0] = "Latency: " + ping.Latency + " ms  Jitter: " + ping.LatencyJitter + " ms";
			netInf[1] = "IP Address: " + client.IpAddress;
			netInf[2] = "Location: " + GeoIP.LookupCountry(client.IpAddress);

			return netInf;
		}

		static string[] SettingsDisplay(Session.Global settings)
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

			return setDisp.ToArray();
		}
	}
}
