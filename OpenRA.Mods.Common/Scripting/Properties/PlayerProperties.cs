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
using System.Linq;
using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Player")]
	public class PlayerProperties : ScriptPlayerProperties
	{
		public PlayerProperties(ScriptContext context, Player player)
			: base(context, player) { }

		[Desc("The player's internal name.")]
		public string InternalName => Player.InternalName;

		[Desc("The player's name.")]
		public string Name => Player.PlayerName;

		[Desc("The player's color.")]
		public Color Color => Player.GetColor(Player);

		[Desc("The player's faction.")]
		public string Faction => Player.Faction.InternalName;

		[Desc("The player's spawnpoint ID.")]
		public int Spawn => Player.SpawnPoint;

		[Desc("The player's home/starting location.")]
		public CPos HomeLocation => Player.HomeLocation;

		[Desc("The player's team ID.")]
		public int Team
		{
			get
			{
				var c = Player.World.LobbyInfo.Clients.FirstOrDefault(i => i.Index == Player.ClientIndex);
				return c?.Team ?? 0;
			}
		}

		[Desc("The player's handicap level.")]
		public int Handicap
		{
			get
			{
				var c = Player.World.LobbyInfo.Clients.FirstOrDefault(i => i.Index == Player.ClientIndex);
				return c?.Handicap ?? 0;
			}
		}

		[Desc("Returns true if the player is a bot.")]
		public bool IsBot => Player.IsBot;

		[Desc("Returns true if the player is non combatant.")]
		public bool IsNonCombatant => Player.NonCombatant;

		[Desc("Returns true if the player is the local player.")]
		public bool IsLocalPlayer => Player == (Player.World.RenderPlayer ?? Player.World.LocalPlayer);

		[Desc("Returns all living actors staying inside the world for this player.")]
		public Actor[] GetActors()
		{
			return Player.World.Actors.Where(actor => actor.Owner == Player && !actor.IsDead && actor.IsInWorld).ToArray();
		}

		[Desc("Returns an array of actors representing all ground attack units of this player.")]
		public Actor[] GetGroundAttackers()
		{
			return Player.World.ActorsHavingTrait<AttackBase>()
				.Where(a => a.Owner == Player && !a.IsDead && a.IsInWorld && a.Info.HasTraitInfo<MobileInfo>())
				.ToArray();
		}

		[Desc("Returns all living actors of the specified type of this player.")]
		public Actor[] GetActorsByType(string type)
		{
			var result = new List<Actor>();

			if (!Context.World.Map.Rules.Actors.TryGetValue(type, out var ai))
				throw new LuaException($"Unknown actor type '{type}'");

			result.AddRange(Player.World.Actors
				.Where(actor => actor.Owner == Player && !actor.IsDead && actor.IsInWorld && actor.Info.Name == ai.Name));

			return result.ToArray();
		}

		[Desc("Returns all living actors of the specified types of this player.")]
		public Actor[] GetActorsByTypes(string[] types)
		{
			var result = new List<Actor>();

			foreach (var type in types)
				if (!Context.World.Map.Rules.Actors.ContainsKey(type))
					throw new LuaException($"Unknown actor type '{type}'");

			result.AddRange(Player.World.Actors
				.Where(actor => actor.Owner == Player && !actor.IsDead && actor.IsInWorld && types.Contains(actor.Info.Name)));

			return result.ToArray();
		}

		[Desc("Check if the player has these prerequisites available.")]
		public bool HasPrerequisites(string[] type)
		{
			var tt = Player.PlayerActor.TraitOrDefault<TechTree>();
			if (tt == null)
				throw new LuaException($"Missing TechTree trait on player {Player}!");

			return tt.HasPrerequisites(type);
		}
	}
}
