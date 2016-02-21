#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA
{
	public enum PowerState { Normal, Low, Critical }
	public enum WinState { Undefined, Won, Lost }

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
		public readonly HSLColor Color;

		public readonly string PlayerName;
		public readonly string InternalName;
		public readonly FactionInfo Faction;
		public readonly bool NonCombatant = false;
		public readonly bool Playable = true;
		public readonly int ClientIndex;
		public readonly PlayerReference PlayerReference;

		/// <summary>The faction (including Random, etc) that was selected in the lobby.</summary>
		public readonly FactionInfo DisplayFaction;

		public WinState WinState = WinState.Undefined;
		public bool IsBot;
		public int SpawnPoint;
		public bool HasObjectives = false;
		public bool Spectating;

		public Shroud Shroud;
		public World World { get; private set; }

		readonly IFogVisibilityModifier[] fogVisibilities;
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
			string botType;

			World = world;
			InternalName = pr.Name;
			PlayerReference = pr;

			// Real player or host-created bot
			if (client != null)
			{
				ClientIndex = client.Index;
				Color = client.Color;
				PlayerName = client.Name;
				botType = client.Bot;
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
				botType = pr.Bot;
				Faction = ChooseFaction(world, pr.Faction, false);
				DisplayFaction = ChooseDisplayFaction(world, pr.Faction);
			}

			PlayerActor = world.CreateActor("Player", new TypeDictionary { new OwnerInit(this) });
			Shroud = PlayerActor.Trait<Shroud>();

			fogVisibilities = PlayerActor.TraitsImplementing<IFogVisibilityModifier>().ToArray();

			// Enable the bot logic on the host
			IsBot = botType != null;
			if (IsBot && Game.IsHost)
			{
				var logic = PlayerActor.TraitsImplementing<IBot>().FirstOrDefault(b => b.Info.Name == botType);
				if (logic == null)
					Log.Write("debug", "Invalid bot type: {0}", botType);
				else
					logic.Activate(this);
			}

			stanceColors.Self = ChromeMetrics.Get<Color>("PlayerStanceColorSelf");
			stanceColors.Allies = ChromeMetrics.Get<Color>("PlayerStanceColorAllies");
			stanceColors.Enemies = ChromeMetrics.Get<Color>("PlayerStanceColorEnemies");
			stanceColors.Neutrals = ChromeMetrics.Get<Color>("PlayerStanceColorNeutrals");
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

		public bool CanViewActor(Actor a)
		{
			return a.CanBeViewedByPlayer(this);
		}

		public bool CanTargetActor(Actor a)
		{
			// PERF: Avoid LINQ.
			if (HasFogVisibility)
				foreach (var fogVisibility in fogVisibilities)
					if (fogVisibility.IsVisible(a))
						return true;

			return CanViewActor(a);
		}

		public bool HasFogVisibility
		{
			get
			{
				// PERF: Avoid LINQ.
				foreach (var fogVisibility in fogVisibilities)
					if (fogVisibility.HasFogVisibility())
						return true;

				return false;
			}
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
