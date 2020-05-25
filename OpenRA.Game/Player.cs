#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		public const string PlayerActorType = "Player";
		public const string EditorPlayerActorType = "EditorPlayer";

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
		public readonly PlayerReference PlayerReference;
		public readonly bool IsBot;
		public readonly string BotType;
		public readonly Shroud Shroud;
		public readonly FrozenActorLayer FrozenActorLayer;

		/// <summary>The faction (including Random, etc) that was selected in the lobby.</summary>
		public readonly FactionInfo DisplayFaction;

		public WinState WinState = WinState.Undefined;
		public int SpawnPoint;
		public bool HasObjectives = false;
		public bool Spectating;

		public World World { get; private set; }

		readonly bool inMissionMap;
		readonly IUnlocksRenderPlayer[] unlockRenderPlayer;

		// Each player is identified with a unique bit in the set
		// Cache masks for the player's index and ally/enemy player indices for performance.
		public LongBitSet<PlayerBitMask> PlayerMask;
		public LongBitSet<PlayerBitMask> AlliedPlayersMask = default(LongBitSet<PlayerBitMask>);
		public LongBitSet<PlayerBitMask> EnemyPlayersMask = default(LongBitSet<PlayerBitMask>);

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

		static FactionInfo ChooseFaction(World world, string name, bool requireSelectable = true)
		{
			var selectableFactions = world.Map.Rules.Actors["world"].TraitInfos<FactionInfo>()
				.Where(f => !requireSelectable || f.Selectable)
				.ToList();

			var selected = selectableFactions.FirstOrDefault(f => f.InternalName == name)
				?? selectableFactions.Random(world.SharedRandom);

			// Don't loop infinite
			for (var i = 0; i <= 10 && selected.RandomFactionMembers.Any(); i++)
			{
				var faction = selected.RandomFactionMembers.Random(world.SharedRandom);
				selected = selectableFactions.FirstOrDefault(f => f.InternalName == faction);

				if (selected == null)
					throw new YamlException("Unknown faction: {0}".F(faction));
			}

			return selected;
		}

		static FactionInfo ChooseDisplayFaction(World world, string factionName)
		{
			var factions = world.Map.Rules.Actors["world"].TraitInfos<FactionInfo>().ToArray();

			return factions.FirstOrDefault(f => f.InternalName == factionName) ?? factions.First();
		}

		public Player(World world, Session.Client client, PlayerReference pr)
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
				if (client.Bot != null)
				{
					var botInfo = world.Map.Rules.Actors["player"].TraitInfos<IBotInfo>().First(b => b.Type == client.Bot);
					var botsOfSameType = world.LobbyInfo.Clients.Where(c => c.Bot == client.Bot).ToArray();
					PlayerName = botsOfSameType.Length == 1 ? botInfo.Name : "{0} {1}".F(botInfo.Name, botsOfSameType.IndexOf(client) + 1);
				}
				else
					PlayerName = client.Name;

				BotType = client.Bot;
				Faction = ChooseFaction(world, client.Faction, !pr.LockFaction);
				DisplayFaction = ChooseDisplayFaction(world, client.Faction);
			}
			else
			{
				// Map player
				ClientIndex = 0; // Owned by the host (TODO: fix this)
				Color = pr.Color;
				PlayerName = pr.Name;
				NonCombatant = pr.NonCombatant;
				Playable = pr.Playable;
				Spectating = pr.Spectating;
				BotType = pr.Bot;
				Faction = ChooseFaction(world, pr.Faction, false);
				DisplayFaction = ChooseDisplayFaction(world, pr.Faction);
			}

			if (!Spectating)
				PlayerMask = new LongBitSet<PlayerBitMask>(InternalName);

			// Set this property before running any Created callbacks on the player actor
			IsBot = BotType != null;

			// Special case handling is required for the Player actor:
			// Since Actor.Created would be called before PlayerActor is assigned here
			// querying player traits in INotifyCreated.Created would crash.
			// Therefore assign the uninitialized actor and run the Created callbacks
			// by calling Initialize ourselves.
			var playerActorType = world.Type == WorldType.Editor ? EditorPlayerActorType : PlayerActorType;
			PlayerActor = new Actor(world, playerActorType, new TypeDictionary { new OwnerInit(this) });
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
		}

		public override string ToString()
		{
			return "{0} ({1})".F(PlayerName, ClientIndex);
		}

		public Dictionary<Player, Stance> Stances = new Dictionary<Player, Stance>();
		public bool IsAlliedWith(Player p)
		{
			// Observers are considered allies to active combatants
			return p == null || Stances[p] == Stance.Ally || (p.Spectating && !NonCombatant);
		}

		public Color PlayerStanceColor(Actor a)
		{
			var player = a.World.RenderPlayer ?? a.World.LocalPlayer;
			if (player != null && !player.Spectating)
			{
				var apparentOwner = a.EffectiveOwner != null && a.EffectiveOwner.Disguised
					? a.EffectiveOwner.Owner
					: a.Owner;

				if (a.Owner.IsAlliedWith(a.World.RenderPlayer))
					apparentOwner = a.Owner;

				if (apparentOwner == player)
					return stanceColors.Self;

				if (apparentOwner.IsAlliedWith(player))
					return stanceColors.Allies;

				if (!apparentOwner.NonCombatant)
					return stanceColors.Enemies;
			}

			return stanceColors.Neutrals;
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
			get { return luaInterface.Value[runtime, keyValue]; }
			set { luaInterface.Value[runtime, keyValue] = value; }
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			Player a, b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				return false;

			return a == b;
		}

		public LuaValue ToString(LuaRuntime runtime)
		{
			return "Player ({0})".F(PlayerName);
		}

		#endregion
	}
}
