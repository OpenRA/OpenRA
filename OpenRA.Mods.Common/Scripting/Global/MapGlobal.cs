#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Map")]
	public class MapGlobal : ScriptGlobal
	{
		SpawnMapActors sma;
		public MapGlobal(ScriptContext context)
			: base(context)
		{
			sma = context.World.WorldActor.Trait<SpawnMapActors>();

			// Register map actors as globals (yuck!)
			foreach (var kv in sma.Actors)
				context.RegisterMapActor(kv.Key, kv.Value);
		}

		[Desc("Returns a table of all actors within the requested region, filtered using the specified function.")]
		public Actor[] ActorsInCircle(WPos location, WRange radius, LuaFunction filter = null)
		{
			var actors = Context.World.FindActorsInCircle(location, radius);

			if (filter != null)
			{
				actors = actors.Where(a =>
				{
					using (var f = filter.Call(a.ToLuaValue(Context)))
						return f.First().ToBoolean();
				});
			}

			return actors.ToArray();
		}

		[Desc("Returns a table of all actors within the requested rectangle, filtered using the specified function.")]
		public Actor[] ActorsInBox(WPos topLeft, WPos bottomRight, LuaFunction filter = null)
		{
			var actors = Context.World.ActorMap.ActorsInBox(topLeft, bottomRight);

			if (filter != null)
			{
				actors = actors.Where(a =>
				{
					using (var f = filter.Call(a.ToLuaValue(Context)))
						return f.First().ToBoolean();
				});
			}

			return actors.ToArray();
		}

		[Desc("Returns the location of the top-left corner of the map.")]
		public WPos TopLeft
		{
			get { return new WPos(Context.World.Map.Bounds.Left * 1024, Context.World.Map.Bounds.Top * 1024, 0); }
		}

		[Desc("Returns the location of the bottom-right corner of the map.")]
		public WPos BottomRight
		{
			get { return new WPos(Context.World.Map.Bounds.Right * 1024, Context.World.Map.Bounds.Bottom * 1024, 0); }
		}

		[Desc("Returns a random cell inside the visible region of the map.")]
		public CPos RandomCell()
		{
			return Context.World.Map.ChooseRandomCell(Context.World.SharedRandom);
		}

		[Desc("Returns a random cell on the visible border of the map.")]
		public CPos RandomEdgeCell()
		{
			return Context.World.Map.ChooseRandomEdgeCell(Context.World.SharedRandom);
		}

		[Desc("Returns the center of a cell in world coordinates.")]
		public WPos CenterOfCell(CPos cell)
		{
			return Context.World.Map.CenterOfCell(cell);
		}

		[Desc("Returns true if there is only one human player.")]
		public bool IsSinglePlayer { get { return Context.World.LobbyInfo.IsSinglePlayer; } }

		[Desc("Returns the difficulty selected by the player before starting the mission.")]
		public string Difficulty { get { return Context.World.LobbyInfo.GlobalSettings.Difficulty; } }

		[Desc("Returns a table of all the actors that were specified in the map file.")]
		public Actor[] NamedActors { get { return sma.Actors.Values.ToArray(); } }

		[Desc("Returns the actor that was specified with a given name in " +
			"the map file (or nil, if the actor is dead or not found).")]
		public Actor NamedActor(string actorName)
		{
			Actor ret;

			if (!sma.Actors.TryGetValue(actorName, out ret))
				return null;

			if (ret.Destroyed)
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
