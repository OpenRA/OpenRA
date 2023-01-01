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

using System.Linq;
using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Map")]
	public class MapGlobal : ScriptGlobal
	{
		readonly SpawnMapActors sma;
		readonly World world;
		readonly GameSettings gameSettings;

		public MapGlobal(ScriptContext context)
			: base(context)
		{
			sma = context.World.WorldActor.Trait<SpawnMapActors>();
			world = context.World;
			gameSettings = Game.Settings.Game;

			// Register map actors as globals (yuck!)
			foreach (var kv in sma.Actors)
				context.RegisterMapActor(kv.Key, kv.Value);
		}

		[Desc("Returns a table of all actors within the requested region, filtered using the specified function.")]
		public Actor[] ActorsInCircle(WPos location, WDist radius, LuaFunction filter = null)
		{
			var actors = Context.World.FindActorsInCircle(location, radius);
			return FilteredObjects(actors, filter).ToArray();
		}

		[Desc("Returns a table of all actors within the requested rectangle, filtered using the specified function.")]
		public Actor[] ActorsInBox(WPos topLeft, WPos bottomRight, LuaFunction filter = null)
		{
			var actors = Context.World.ActorMap.ActorsInBox(topLeft, bottomRight);
			return FilteredObjects(actors, filter).ToArray();
		}

		// HACK: This api method abuses the coordinate system, and should be removed
		// in favour of proper actor queries.  See #8549.
		[Desc("Returns the location of the top-left corner of the map (assuming zero terrain height).")]
		public WPos TopLeft => Context.World.Map.ProjectedTopLeft;

		// HACK: This api method abuses the coordinate system, and should be removed
		// in favour of proper actor queries.  See #8549.
		[Desc("Returns the location of the bottom-right corner of the map (assuming zero terrain height).")]
		public WPos BottomRight => Context.World.Map.ProjectedBottomRight;

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

		[Desc("Returns the closest cell on the visible border of the map from the given cell.")]
		public CPos ClosestEdgeCell(CPos givenCell)
		{
			return Context.World.Map.ChooseClosestEdgeCell(givenCell);
		}

		[Desc("Returns the first cell on the visible border of the map from the given cell,",
			"matching the filter function called as function(CPos cell).")]
		public CPos ClosestMatchingEdgeCell(CPos givenCell, LuaFunction filter)
		{
			return FilteredObjects(Context.World.Map.AllEdgeCells.OrderBy(c => (givenCell - c).Length), filter).FirstOrDefault();
		}

		[Desc("Returns the center of a cell in world coordinates.")]
		public WPos CenterOfCell(CPos cell)
		{
			return Context.World.Map.CenterOfCell(cell);
		}

		[Desc("Returns the type of the terrain at the target cell.")]
		public string TerrainType(CPos cell)
		{
			return Context.World.Map.GetTerrainInfo(cell).Type;
		}

		[Desc("Returns true if there is only one human player.")]
		public bool IsSinglePlayer => Context.World.LobbyInfo.NonBotPlayers.Count() == 1;

		[Desc("Returns true if this is a shellmap and the player has paused animations.")]
		public bool IsPausedShellmap => Context.World.Type == WorldType.Shellmap && gameSettings.PauseShellmap;

		[Desc("Returns the value of a `ScriptLobbyDropdown` selected in the game lobby.")]
		public LuaValue LobbyOption(string id)
		{
			var option = Context.World.WorldActor.TraitsImplementing<ScriptLobbyDropdown>()
				.FirstOrDefault(sld => sld.Info.ID == id);

			if (option == null)
			{
				Log.Write("lua", "A ScriptLobbyDropdown with ID `" + id + "` was not found.");
				return null;
			}

			return option.Value;
		}

		[Desc("Returns a table of all the actors that were specified in the map file.")]
		public Actor[] NamedActors => sma.Actors.Values.ToArray();

		[Desc("Returns the actor that was specified with a given name in " +
		      "the map file (or nil, if the actor is dead or not found).")]
		public Actor NamedActor(string actorName)
		{
			if (!sma.Actors.TryGetValue(actorName, out var ret))
				return null;

			if (ret.Disposed)
				return null;

			return ret;
		}

		[Desc("Returns true if actor was originally specified in the map file.")]
		public bool IsNamedActor(Actor actor)
		{
			return actor.ActorID <= sma.LastMapActorID && actor.ActorID > sma.LastMapActorID - sma.Actors.Count;
		}

		[Desc("Returns a table of all actors tagged with the given string.")]
		public Actor[] ActorsWithTag(string tag)
		{
			return Context.World.ActorsHavingTrait<ScriptTags>(t => t.HasTag(tag)).ToArray();
		}

		[Desc("Returns a table of all the actors that are currently on the map/in the world.")]
		public Actor[] ActorsInWorld => world.Actors.ToArray();
	}
}
