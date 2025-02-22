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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Server;
using OpenRA.Traits;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class SkirmishLogic : ServerTrait, IClientJoined, INotifySyncLobbyInfo
	{
		sealed class SkirmishSlot
		{
			[FieldLoader.Serialize(FromYamlKey = true)]
			public readonly string Slot;
			public readonly Color Color;
			public readonly string Faction;
			public readonly int SpawnPoint;
			public readonly int Team;
			public readonly int Handicap;

			public SkirmishSlot() { }

			public SkirmishSlot(Session.Client c)
			{
				Slot = c.Slot;
				Color = c.Color;
				Faction = c.Faction;
				SpawnPoint = c.SpawnPoint;
				Team = c.Team;
				Handicap = c.Handicap;
			}

			public static void DeserializeToClient(MiniYaml yaml, Session.Client c)
			{
				var s = FieldLoader.Load<SkirmishSlot>(yaml);
				c.Slot = s.Slot;
				c.Color = c.PreferredColor = s.Color;
				c.Faction = s.Faction;
				c.SpawnPoint = s.SpawnPoint;
				c.Team = s.Team;
				c.Handicap = s.Handicap;
			}
		}

		static bool TryInitializeFromFile(S server, string path, Connection conn)
		{
			if (!File.Exists(path))
				return false;

			var nodes = new MiniYaml("", MiniYaml.FromFile(path));
			var mapNode = nodes.NodeWithKeyOrDefault("Map");
			if (mapNode == null)
				return false;

			// Only set players and options if the map is available
			if (server.LobbyInfo.GlobalSettings.Map != mapNode.Value.Value)
			{
				var map = server.ModData.MapCache[mapNode.Value.Value];
				if (map.Status != MapStatus.Available || !server.InterpretCommand($"map {map.Uid}", conn))
					return false;
			}

			var optionsNode = nodes.NodeWithKeyOrDefault("Options");
			if (optionsNode != null)
			{
				var options = server.Map.PlayerActorInfo.TraitInfos<ILobbyOptions>()
					.Concat(server.Map.WorldActorInfo.TraitInfos<ILobbyOptions>())
					.SelectMany(t => t.LobbyOptions(server.Map))
					.ToDictionary(o => o.Id, o => o);

				foreach (var optionNode in optionsNode.Value.Nodes)
				{
					if (options.TryGetValue(optionNode.Key, out var option) && !option.IsLocked && option.Values.ContainsKey(optionNode.Value.Value))
					{
						var oo = server.LobbyInfo.GlobalSettings.LobbyOptions[option.Id];
						oo.Value = oo.PreferredValue = optionNode.Value.Value;
					}
				}
			}

			var selectableFactions = server.Map.WorldActorInfo.TraitInfos<FactionInfo>()
				.Where(f => f.Selectable)
				.Select(f => f.InternalName)
				.ToList();

			var playerNode = nodes.NodeWithKeyOrDefault("Player");
			if (playerNode != null)
			{
				var client = server.GetClient(conn);
				SkirmishSlot.DeserializeToClient(playerNode.Value, client);
				client.Color = LobbyCommands.SanitizePlayerColor(server, client.Color, client.Index);
				client.Faction = LobbyCommands.SanitizePlayerFaction(server, client.Faction, selectableFactions);
			}

			var botsNode = nodes.NodeWithKeyOrDefault("Bots");
			if (botsNode != null)
			{
				var botController = server.LobbyInfo.Clients.First(c => c.IsAdmin);
				foreach (var botNode in botsNode.Value.Nodes)
				{
					var botInfo = server.Map.PlayerActorInfo.TraitInfos<IBotInfo>()
						.FirstOrDefault(b => b.Type == botNode.Key);

					if (botInfo == null)
						continue;

					var client = new Session.Client
					{
						Index = server.ChooseFreePlayerIndex(),
						Name = botInfo.Name,
						Bot = botInfo.Type,
						Slot = botNode.Value.Value,
						State = Session.ClientState.NotReady,
						BotControllerClientIndex = botController.Index
					};

					SkirmishSlot.DeserializeToClient(botNode.Value, client);

					// Validate whether color is allowed and get an alternative if it isn't
					if (client.Slot != null && !server.LobbyInfo.Slots[client.Slot].LockColor)
						client.Color = LobbyCommands.SanitizePlayerColor(server, client.Color, client.Index);

					client.Faction = LobbyCommands.SanitizePlayerFaction(server, client.Faction, selectableFactions);

					server.LobbyInfo.Clients.Add(client);
					S.SyncClientToPlayerReference(client, server.Map.Players.Players[client.Slot]);
				}
			}

			return true;
		}

		void INotifySyncLobbyInfo.LobbyInfoSynced(S server)
		{
			if (server.Type != ServerType.Skirmish)
				return;

			var path = Path.Combine(Platform.SupportDir, $"skirmish.{server.ModData.Manifest.Id}.yaml");
			var playerClient = server.LobbyInfo.NonBotClients.First();
			new List<MiniYamlNode>
			{
				new("Map", server.LobbyInfo.GlobalSettings.Map),
				new("Options", new MiniYaml("", server.LobbyInfo.GlobalSettings.LobbyOptions
					.Select(kv => new MiniYamlNode(kv.Key, kv.Value.Value)))),
				new("Player", FieldSaver.Save(new SkirmishSlot(playerClient))),
				new("Bots", new MiniYaml("", server.LobbyInfo.Clients.Where(c => c.IsBot)
					.Select(b => new MiniYamlNode(b.Bot, FieldSaver.Save(new SkirmishSlot(b))))))
			}.WriteToFile(path);
		}

		void IClientJoined.ClientJoined(S server, Connection conn)
		{
			if (server.Type != ServerType.Skirmish)
				return;

			var skirmishFile = Path.Combine(Platform.SupportDir, $"skirmish.{server.ModData.Manifest.Id}.yaml");
			if (TryInitializeFromFile(server, skirmishFile, conn))
				return;

			var slot = server.LobbyInfo.FirstEmptyBotSlot();
			var bot = server.Map.PlayerActorInfo.TraitInfos<IBotInfo>().Select(t => t.Type).FirstOrDefault();
			var botController = server.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
			if (slot != null && bot != null)
				server.InterpretCommand($"slot_bot {slot} {botController.Index} {bot}", conn);
		}
	}
}
