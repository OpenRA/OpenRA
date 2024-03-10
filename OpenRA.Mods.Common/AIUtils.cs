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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public enum BuildingType { Building, Defense, Refinery }

	public enum WaterCheck { NotChecked, EnoughWater, NotEnoughWater, DontCheck }

	public static class AIUtils
	{
		public static bool IsAreaAvailable<T>(World world, Player player, Map map, int radius, HashSet<string> terrainTypes)
		{
			var cells = world.ActorsHavingTrait<T>().Where(a => a.Owner == player);

			// TODO: Properly check building foundation rather than 3x3 area.
			return cells.Select(a => map.FindTilesInCircle(a.Location, radius)
				.Count(c => map.Contains(c) && terrainTypes.Contains(map.GetTerrainInfo(c).Type) &&
					Util.AdjacentCells(world, Target.FromCell(world, c))
						.All(ac => map.Contains(ac) && terrainTypes.Contains(map.GetTerrainInfo(ac).Type))))
							.Any(availableCells => availableCells > 0);
		}

		public static IEnumerable<ProductionQueue> FindQueues(Player player, string category)
		{
			return player.World.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player && a.Trait.Info.Type == category && a.Trait.Enabled)
				.Select(a => a.Trait);
		}

		public static int CountActorsWithNameAndTrait<T>(string actorName, Player owner)
		{
			return owner.World.ActorsHavingTrait<T>().Count(a => a.Owner == owner && a.Info.Name == actorName);
		}

		public static int CountActorByCommonName<T>(ActorIndex.OwnerAndNamesAndTrait<T> actorIndex)
		{
			return actorIndex.Actors.Count(a => !a.IsDead);
		}

		public static void BotDebug(string format, params object[] args)
		{
			if (Game.Settings.Debug.BotDebug)
				TextNotificationsManager.Debug(format, args);
		}

		public static IEnumerable<Order> ClearBlockersOrders(List<CPos> tiles, Player owner, Actor ignoreActor = null)
		{
			var world = owner.World;
			var adjacentTiles = Util.ExpandFootprint(tiles, true).Except(tiles)
				.Where(world.Map.Contains).ToList();

			var blockers = tiles.SelectMany(world.ActorMap.GetActorsAt)
				.Where(a => a.Owner == owner && a.IsIdle && (ignoreActor == null || a != ignoreActor))
				.Select(a => new TraitPair<IMove>(a, a.TraitOrDefault<IMove>()))
				.Where(x => x.Trait != null);

			foreach (var blocker in blockers)
			{
				CPos moveCell;
				if (blocker.Trait is Mobile mobile)
				{
					var availableCells = adjacentTiles.Where(t => mobile.CanEnterCell(t)).ToList();
					if (availableCells.Count == 0)
						continue;

					moveCell = blocker.Actor.ClosestCell(availableCells);
				}
				else if (blocker.Trait is Aircraft)
					moveCell = blocker.Actor.Location;
				else
					continue;

				yield return new Order("Move", blocker.Actor, Target.FromCell(world, moveCell), false)
				{
					SuppressVisualFeedback = true
				};
			}
		}
	}
}
