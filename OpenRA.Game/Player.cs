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
using System.Linq;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA
{
	public enum PowerState { Normal, Low, Critical }
	public enum WinState { Undefined, Won, Lost }

	public class Player : IScriptBindable, IScriptNotifyBind, ILuaTableBinding, ILuaEqualityBinding, ILuaToStringBinding
	{
		public readonly Actor PlayerActor;
		public readonly HSLColor Color;

		public readonly string PlayerName;
		public readonly string InternalName;
		public readonly FactionInfo Country;
		public readonly bool NonCombatant = false;
		public readonly bool Playable = true;
		public readonly int ClientIndex;
		public readonly PlayerReference PlayerReference;

		// The country (including Random, etc) that was selected in the lobby
		public readonly FactionInfo DisplayCountry;

		public WinState WinState = WinState.Undefined;
		public bool IsBot;
		public int SpawnPoint;
		public bool HasObjectives = false;
		public bool Spectating;

		public Shroud Shroud;
		public World World { get; private set; }

		readonly IFogVisibilityModifier[] fogVisibilities;

		FactionInfo ChooseCountry(World world, string name, bool requireSelectable = true)
		{
			var selectableCountries = world.Map.Rules.Actors["world"].Traits
				.WithInterface<FactionInfo>().Where(f => !requireSelectable || f.Selectable)
				.ToList();

			var selected = selectableCountries.FirstOrDefault(f => f.InternalName == name)
				?? selectableCountries.Random(world.SharedRandom);

			// Don't loop infinite
			for (var i = 0; i <= 10 && selected.RandomFactionMembers.Any(); i++)
			{
				var race = selected.RandomFactionMembers.Random(world.SharedRandom);
				selected = selectableCountries.FirstOrDefault(f => f.InternalName == race);

				if (selected == null)
					throw new YamlException("Unknown race: {0}".F(race));
			}

			return selected;
		}

		FactionInfo ChooseDisplayCountry(World world, string race)
		{
			var countries = world.Map.Rules.Actors["world"].Traits
				.WithInterface<FactionInfo>().ToArray();

			return countries.FirstOrDefault(f => f.InternalName == race) ?? countries.First();
		}

		public Player(World world, Session.Client client, Session.Slot slot, PlayerReference pr)
		{
			World = world;
			InternalName = pr.Name;
			PlayerReference = pr;
			string botType = null;

			// Real player or host-created bot
			if (client != null)
			{
				ClientIndex = client.Index;
				Color = client.Color;
				PlayerName = client.Name;
				botType = client.Bot;
				Country = ChooseCountry(world, client.Race, !pr.LockFaction);
				DisplayCountry = ChooseDisplayCountry(world, client.Race);
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
				Country = ChooseCountry(world, pr.Faction, false);
				DisplayCountry = ChooseDisplayCountry(world, pr.Faction);
			}

			PlayerActor = world.CreateActor("Player", new TypeDictionary { new OwnerInit(this) });
			Shroud = PlayerActor.Trait<Shroud>();

			fogVisibilities = PlayerActor.TraitsImplementing<IFogVisibilityModifier>()
				.ToArray();

			// Enable the bot logic on the host
			IsBot = botType != null;
			if (IsBot && Game.IsHost)
			{
				var logic = PlayerActor.TraitsImplementing<IBot>()
							.FirstOrDefault(b => b.Info.Name == botType);
				if (logic == null)
					Log.Write("debug", "Invalid bot type: {0}", botType);
				else
					logic.Activate(this);
			}
		}

		public override string ToString()
		{
			return "{0} ({1})".F(PlayerName, ClientIndex);
		}

		public Dictionary<Player, Stance> Stances = new Dictionary<Player, Stance>();
		public bool IsAlliedWith(Player p)
		{
			// Observers are considered as allies
			return p == null || Stances[p] == Stance.Ally || p.Spectating;
		}

		public void SetStance(Player target, Stance s)
		{
			var oldStance = Stances[target];
			Stances[target] = s;
			target.Shroud.UpdatePlayerStance(World, this, oldStance, s);
			Shroud.UpdatePlayerStance(World, target, oldStance, s);

			foreach (var nsc in World.ActorsWithTrait<INotifyStanceChanged>())
				nsc.Trait.StanceChanged(nsc.Actor, this, target, oldStance, s);
		}

		public bool CanViewActor(Actor a)
		{
			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(a, this)))
				return false;

			return a.Trait<IDefaultVisibility>().IsVisible(a, this);
		}

		public bool CanTargetActor(Actor a)
		{
			if (HasFogVisibility)
				return true;

			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(a, this)))
				return false;

			return a.Trait<IDefaultVisibility>().IsVisible(a, this);
		}

		public bool HasFogVisibility { get { return fogVisibilities.Any(f => f.HasFogVisibility(this)); } }

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
			if (!left.TryGetClrValue<Player>(out a) || !right.TryGetClrValue<Player>(out b))
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
