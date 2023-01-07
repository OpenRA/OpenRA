#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using DiscordRPC;
using DiscordRPC.Message;
using OpenRA.Network;

namespace OpenRA.Mods.Common
{
	public enum DiscordState
	{
		Unknown,
		InMenu,
		InMapEditor,
		InSkirmishLobby,
		InMultiplayerLobby,
		PlayingMultiplayer,
		WatchingReplay,
		PlayingCampaign,
		PlayingSkirmish
	}

	public sealed class DiscordService : IGlobalModData, IDisposable
	{
		public readonly string ApplicationId = null;
		public readonly string Tooltip = "Open Source real-time strategy game engine for early Westwood titles.";
		DiscordRpcClient client;
		DiscordState currentState;

		static DiscordService instance;
		static DiscordService Service
		{
			get
			{
				if (instance != null)
					return instance;

				if (!Game.Settings.Game.EnableDiscordService)
					return null;

				if (!Game.ModData.Manifest.Contains<DiscordService>())
					return null;

				instance = Game.ModData.Manifest.Get<DiscordService>();
				return instance;
			}
		}

		public DiscordService(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);

			if (!Game.Settings.Game.EnableDiscordService)
				return;

			// HACK: Prevent service from starting when launching the utility or server.
			if (Game.Renderer == null)
				return;

			client = new DiscordRpcClient(ApplicationId, autoEvents: true)
			{
				SkipIdenticalPresence = false
			};

			client.OnJoin += OnJoin;
			client.OnJoinRequested += OnJoinRequested;

			// HACK: We need to set HasRegisteredUriScheme to bypass the check that is done when calling SetPresence with a joinSecret.
			// DiscordRpc lib expect us to register uri handlers with RegisterUriScheme(), we are doing it ourselves in our installers/launchers.
			client.GetType().GetProperty(nameof(DiscordRpcClient.HasRegisteredUriScheme)).SetValue(client, true);

			client.SetSubscription(EventType.Join | EventType.JoinRequest);
			client.Initialize();

			// Set an initial value for the rich presence to avoid a NRE in the library
			client.SetPresence(new RichPresence());
		}

		void OnJoinRequested(object sender, JoinRequestMessage args)
		{
			var client = (DiscordRpcClient)sender;
			client.Respond(args, true);
		}

		void OnJoin(object sender, JoinMessage args)
		{
			if (currentState != DiscordState.InMenu)
				return;

			var server = args.Secret.Split('|');
			Game.RunAfterTick(() =>
			{
				Game.RemoteDirectConnect(new ConnectionTarget(server[0], int.Parse(server[1])));
			});
		}

		void SetStatus(DiscordState state, string details = null, string secret = null, int? players = null, int? slots = null)
		{
			if (currentState == state)
				return;

			if (instance == null)
				return;

			string stateText;
			DateTime? timestamp = null;
			Party party = null;
			Secrets secrets = null;
			Button[] buttons = null;

			switch (state)
			{
				case DiscordState.InMenu:
					stateText = "In menu";
					break;
				case DiscordState.InMapEditor:
					stateText = "In Map Editor";
					break;
				case DiscordState.InSkirmishLobby:
					stateText = "In Skirmish Lobby";
					break;
				case DiscordState.InMultiplayerLobby:
					stateText = "In Multiplayer Lobby";
					timestamp = DateTime.UtcNow;
					party = new Party
					{
						ID = Secrets.CreateFriendlySecret(new Random()),
						Size = players.Value,
						Max = slots.Value
					};
					secrets = new Secrets
					{
						JoinSecret = secret
					};
					break;
				case DiscordState.PlayingMultiplayer:
					stateText = "Playing Multiplayer";
					timestamp = DateTime.UtcNow;
					break;
				case DiscordState.PlayingCampaign:
					stateText = "Playing Campaign";
					timestamp = DateTime.UtcNow;
					break;
				case DiscordState.WatchingReplay:
					stateText = "Watching Replay";
					timestamp = DateTime.UtcNow;
					break;
				case DiscordState.PlayingSkirmish:
					stateText = "Playing Skirmish";
					timestamp = DateTime.UtcNow;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, null);
			}

			if (party == null)
			{
				buttons = new[]
				{
					new Button
					{
						Label = "Visit Website",
						Url = Game.ModData.Manifest.Metadata.Website
					}
				};
			}

			var richPresence = new RichPresence
			{
				Details = details,
				State = stateText,
				Assets = new Assets
				{
					LargeImageKey = "large",
					LargeImageText = Tooltip,
				},
				Timestamps = timestamp.HasValue ? new Timestamps(timestamp.Value) : null,
				Party = party,
				Secrets = secrets,
				Buttons = buttons
			};

			client.SetPresence(richPresence);
			currentState = state;
		}

		void UpdateParty(int players, int slots)
		{
			if (client.CurrentPresence.Party != null)
			{
				client.UpdatePartySize(players, slots);
				return;
			}

			client.UpdateParty(new Party
			{
				ID = Secrets.CreateFriendlySecret(new Random()),
				Size = players,
				Max = slots
			});
		}

		public void Dispose()
		{
			if (client != null)
			{
				client.Dispose();
				client = null;
				instance = null;
			}
		}

		public static void UpdateStatus(DiscordState state, string details = null, string secret = null, int? players = null, int? slots = null)
		{
			Service?.SetStatus(state, details, secret, players, slots);
		}

		public static void UpdatePlayers(int players, int slots)
		{
			Service?.UpdateParty(players, slots);
		}

		public static void UpdateDetails(string details)
		{
			Service?.client.UpdateDetails(details);
		}
	}
}
