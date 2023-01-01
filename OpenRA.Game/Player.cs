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
using System.Linq;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA
{
	[Flags]
	public enum PowerState
	{
		Normal = 1,
		Low = 2,
		Critical = 4
	}

	public enum WinState { Undefined, Won, Lost }

	public class PlayerBitMask { }

	public class Player : IScriptBindable, IScriptNotifyBind, ILuaTableBinding, ILuaEqualityBinding, ILuaToStringBinding
	{
		struct StanceColors
		{
			public Color Self;
			public Color Allies;
			public Color Enemies;
			public Color Neutrals;
		}

		public readonly Actor PlayerActor;
		public readonly Color Color;

		public readonly string PlayerName;
		public readonly string InternalName;
		public readonly FactionInfo Faction;
		public readonly bool NonCombatant = false;
		public readonly bool Playable = true;
		public readonly int ClientIndex;
		public readonly CPos HomeLocation;
		public readonly int Handicap;
		public readonly PlayerReference PlayerReference;
		public readonly bool IsBot;
		public readonly string BotType;
		public readonly Shroud Shroud;
		public readonly FrozenActorLayer FrozenActorLayer;

		/// <summary>The faction (including Random, etc.) that was selected in the lobby.</summary>
		public readonly FactionInfo DisplayFaction;

		/// <summary>The spawn point index that was assigned for client-based players.</summary>
		public readonly int SpawnPoint;

		/// <summary>The spawn point index (including 0 for Random) that was selected in the lobby for client-based players.</summary>
		public readonly int DisplaySpawnPoint;

		public WinState WinState = WinState.Undefined;
		public bool HasObjectives = false;

		// Players in mission maps must not leave the player view
		public bool Spectating => !inMissionMap && (spectating || WinState != WinState.Undefined);

		public World World { get; }

		readonly bool inMissionMap;
		readonly bool spectating;
		readonly IUnlocksRenderPlayer[] unlockRenderPlayer;
		readonly INotifyPlayerDisconnected[] notifyDisconnected;

		// Each player is identified with a unique bit in the set
		// Cache masks for the player's index and ally/enemy player indices for performance.
		public LongBitSet<PlayerBitMask> PlayerMask;
		public LongBitSet<PlayerBitMask> AlliedPlayersMask = default;
		public LongBitSet<PlayerBitMask> EnemyPlayersMask = default;

		public bool UnlockedRenderPlayer
		{
			get
			{
				if (unlockRenderPlayer.Any(x => x.RenderPlayerUnlocked))
					return true;

				return WinState != WinState.Undefined && !inMissionMap;
			}
		}

		readonly StanceColors stanceColors;

		public static FactionInfo ResolveFaction(string factionName, IEnumerable<FactionInfo> factionInfos, MersenneTwister playerRandom, bool requireSelectable = true)
		{
			var selectableFactions = factionInfos
				.Where(f => !requireSelectable || f.Selectable)
				.ToList();

			var selected = selectableFactions.FirstOrDefault(f => f.InternalName == factionName)
				?? selectableFactions.Random(playerRandom);

			// Don't loop infinite
			for (var i = 0; i <= 10 && selected.RandomFactionMembers.Count > 0; i++)
			{
				var faction = selected.RandomFactionMembers.Random(playerRandom);
				selected = selectableFactions.FirstOrDefault(f => f.InternalName == faction);

				if (selected == null)
					throw new YamlException($"Unknown faction: {faction}");
			}

			return selected;
		}

		static FactionInfo ResolveFaction(World world, string factionName, MersenneTwister playerRandom, bool requireSelectable)
		{
			var factionInfos = world.Map.Rules.Actors[SystemActors.World].TraitInfos<FactionInfo>();
			return ResolveFaction(factionName, factionInfos, playerRandom, requireSelectable);
		}

		static FactionInfo ResolveDisplayFaction(World world, string factionName)
		{
			var factions = world.Map.Rules.Actors[SystemActors.World].TraitInfos<FactionInfo>().ToArray();

			return factions.FirstOrDefault(f => f.InternalName == factionName) ?? factions.First();
		}

		public static string ResolvePlayerName(Session.Client client, IEnumerable<Session.Client> clients, IEnumerable<IBotInfo> botInfos)
		{
			if (client.Bot != null)
			{
				var botInfo = botInfos.First(b => b.Type == client.Bot);
				var botsOfSameType = clients.Where(c => c.Bot == client.Bot).ToArray();
				return botsOfSameType.Length == 1 ? botInfo.Name : $"{botInfo.Name} {botsOfSameType.IndexOf(client) + 1}";
			}

			return client.Name;
		}

		public Player(World world, Session.Client client, PlayerReference pr, MersenneTwister playerRandom)
		{
			World = world;
			InternalName = pr.Name;
			PlayerReference = pr;

			inMissionMap = world.Map.Visibility.HasFlag(MapVisibility.MissionSelector);

			// Real player or host-created bot
			if (client != null)
			{
				ClientIndex = client.Index;
				Color = client.Color;
				PlayerName = ResolvePlayerName(client, world.LobbyInfo.Clients, world.Map.Rules.Actors[SystemActors.Player].TraitInfos<IBotInfo>());

				BotType = client.Bot;
				Faction = ResolveFaction(world, client.Faction, playerRandom, !pr.LockFaction);
				DisplayFaction = ResolveDisplayFaction(world, client.Faction);

				var assignSpawnPoints = world.WorldActor.TraitOrDefault<IAssignSpawnPoints>();
				HomeLocation = assignSpawnPoints?.AssignHomeLocation(world, client, playerRandom) ?? pr.HomeLocation;
				SpawnPoint = assignSpawnPoints?.SpawnPointForPlayer(this) ?? client.SpawnPoint;
				DisplaySpawnPoint = client.SpawnPoint;

				Handicap = client.Handicap;
			}
			else
			{
				// Map player
				ClientIndex = world.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin)?.Index ?? 0; // Owned by the host (TODO: fix this)
				Color = pr.Color;
				PlayerName = pr.Name;
				NonCombatant = pr.NonCombatant;
				Playable = pr.Playable;
				spectating = pr.Spectating;
				BotType = pr.Bot;
				Faction = ResolveFaction(world, pr.Faction, playerRandom, false);
				DisplayFaction = ResolveDisplayFaction(world, pr.Faction);
				HomeLocation = pr.HomeLocation;
				SpawnPoint = DisplaySpawnPoint = 0;
				Handicap = pr.Handicap;
			}

			if (!spectating)
				PlayerMask = new LongBitSet<PlayerBitMask>(InternalName);

			// Set this property before running any Created callbacks on the player actor
			IsBot = BotType != null;

			// Special case handling is required for the Player actor:
			// Since Actor.Created would be called before PlayerActor is assigned here
			// querying player traits in INotifyCreated.Created would crash.
			// Therefore assign the uninitialized actor and run the Created callbacks
			// by calling Initialize ourselves.
			var playerActorType = world.Type == WorldType.Editor ? SystemActors.EditorPlayer : SystemActors.Player;
			PlayerActor = new Actor(world, playerActorType.ToString(), new TypeDictionary { new OwnerInit(this) });
			PlayerActor.Initialize(true);

			Shroud = PlayerActor.Trait<Shroud>();
			FrozenActorLayer = PlayerActor.TraitOrDefault<FrozenActorLayer>();

			// Enable the bot logic on the host
			if (IsBot && Game.IsHost)
			{
				var logic = PlayerActor.TraitsImplementing<IBot>().FirstOrDefault(b => b.Info.Type == BotType);
				if (logic == null)
					Log.Write("debug", "Invalid bot type: {0}", BotType);
				else
					logic.Activate(this);
			}

			stanceColors.Self = ChromeMetrics.Get<Color>("PlayerStanceColorSelf");
			stanceColors.Allies = ChromeMetrics.Get<Color>("PlayerStanceColorAllies");
			stanceColors.Enemies = ChromeMetrics.Get<Color>("PlayerStanceColorEnemies");
			stanceColors.Neutrals = ChromeMetrics.Get<Color>("PlayerStanceColorNeutrals");

			unlockRenderPlayer = PlayerActor.TraitsImplementing<IUnlocksRenderPlayer>().ToArray();
			notifyDisconnected = PlayerActor.TraitsImplementing<INotifyPlayerDisconnected>().ToArray();
		}

		public override string ToString()
		{
			return $"{PlayerName} ({ClientIndex})";
		}

		public PlayerRelationship RelationshipWith(Player other)
		{
			if (this == other)
				return PlayerRelationship.Ally;

			// Observers are considered allies to active combatants
			if (other == null || other.Spectating)
				return NonCombatant ? PlayerRelationship.Neutral : PlayerRelationship.Ally;

			if (AlliedPlayersMask.Overlaps(other.PlayerMask))
				return PlayerRelationship.Ally;

			if (EnemyPlayersMask.Overlaps(other.PlayerMask))
				return PlayerRelationship.Enemy;

			return PlayerRelationship.Neutral;
		}

		/// <summary> returns true if player is null </summary>
		public bool IsAlliedWith(Player p)
		{
			return RelationshipWith(p) == PlayerRelationship.Ally;
		}

		public Color PlayerRelationshipColor(Actor a)
		{
			var renderPlayer = a.World.RenderPlayer;
			var player = renderPlayer ?? a.World.LocalPlayer;
			if (player != null && !player.Spectating)
			{
				var effectiveOwner = a.EffectiveOwner;
				var apparentOwner = a.Owner;
				if (effectiveOwner != null && effectiveOwner.Disguised && !a.Owner.IsAlliedWith(renderPlayer))
					apparentOwner = effectiveOwner.Owner;

				if (apparentOwner == player)
					return stanceColors.Self;

				if (apparentOwner.IsAlliedWith(player))
					return stanceColors.Allies;

				if (!apparentOwner.NonCombatant)
					return stanceColors.Enemies;
			}

			return stanceColors.Neutrals;
		}

		internal void PlayerDisconnected(Player p)
		{
			foreach (var np in notifyDisconnected)
				np.PlayerDisconnected(PlayerActor, p);
		}

		#region Scripting interface

		Lazy<ScriptPlayerInterface> luaInterface;
		public void OnScriptBind(ScriptContext context)
		{
			if (luaInterface == null)
				luaInterface = Exts.Lazy(() => new ScriptPlayerInterface(context, this));
		}

		public LuaValue this[LuaRuntime runtime, LuaValue keyValue]
		{
			get => luaInterface.Value[runtime, keyValue];
			set => luaInterface.Value[runtime, keyValue] = value;
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			if (!left.TryGetClrValue(out Player a) || !right.TryGetClrValue(out Player b))
				return false;

			return a == b;
		}

		public LuaValue ToString(LuaRuntime runtime)
		{
			return $"Player ({PlayerName})";
		}

		#endregion
	}
}
