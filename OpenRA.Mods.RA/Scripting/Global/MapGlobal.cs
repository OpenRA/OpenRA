#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using Eluant;
using OpenRA.Scripting;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptGlobal("Map")]
	public class MapGlobal : ScriptGlobal
	{
		SpawnMapActors sma;
		public MapGlobal(ScriptContext context) : base(context)
		{
			sma = context.World.WorldActor.Trait<SpawnMapActors>();

			// Register map actors as globals (yuck!)
			foreach (var kv in sma.Actors)
				context.RegisterMapActor(kv.Key, kv.Value);
		}

		[Desc("Returns a table of all actors within the requested region, filtered using the specified function.")]
		public LuaTable ActorsInCircle(WPos location, WRange radius, LuaFunction filter)
		{
			var actors = context.World.FindActorsInCircle(location, radius)
				.Select(a => a.ToLuaValue(context))
				.Where(a => 
				{
					using (var f = filter.Call(a))
						return f.First().ToBoolean();
				});
			return actors.ToLuaTable(context);
		}

		[Desc("Returns a random cell inside the visible region of the map.")]
		public CPos RandomCell()
		{
			return context.World.Map.ChooseRandomCell(context.World.SharedRandom);
		}

		[Desc("Returns a random cell on the visible border of the map.")]
		public CPos RandomEdgeCell()
		{
			return context.World.Map.ChooseRandomEdgeCell(context.World.SharedRandom);
		}

		[Desc("Returns true if there is only one human player.")]
		public bool IsSinglePlayer { get { return context.World.LobbyInfo.IsSinglePlayer; } }

		[Desc("Returns the difficulty selected by the player before starting the mission.")]
		public string Difficulty { get { return context.World.LobbyInfo.GlobalSettings.Difficulty; } }

		[Desc("Returns a table of all the actors that were specified in the map file.")]
		public LuaTable NamedActors
		{
			get
			{
				return sma.Actors.Values.ToLuaTable(context);
			}
		}

		[Desc("Returns the actor that was specified with a given name in " +
			"the map file (or nil, if the actor is dead or not found")]
		public Actor NamedActor(string actorName)
		{
			Actor ret;
			if (!sma.Actors.TryGetValue(actorName, out ret))
				return null;

			return ret;
		}

		[Desc("Returns true if actor was originally specified in the map file.")]
		public bool IsNamedActor(Actor actor)
		{
			return actor.ActorID <= sma.LastMapActorID && actor.ActorID > sma.LastMapActorID - sma.Actors.Count;
		}
	}
}
