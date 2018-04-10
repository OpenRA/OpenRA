#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	public enum BuildingType { Building, Defense, Refinery }

	public class CaptureTarget<TInfoType> where TInfoType : class, ITraitInfoInterface
	{
		internal readonly Actor Actor;
		internal readonly TInfoType Info;

		/// <summary>The order string given to the capturer so they can capture this actor.</summary>
		/// <example>ExternalCaptureActor</example>
		internal readonly string OrderString;

		internal CaptureTarget(Actor actor, string orderString)
		{
			Actor = actor;
			Info = actor.Info.TraitInfoOrDefault<TInfoType>();
			OrderString = orderString;
		}
	}

	public static class AIUtils
	{
		public static bool IsAreaAvailable<T>(World world, Player player, Map map, int radius, HashSet<string> terrainTypes)
		{
			var cells = world.ActorsHavingTrait<T>().Where(a => a.Owner == player);

			// TODO: Properly check building foundation rather than 3x3 area.
			return cells.Select(a => map.FindTilesInCircle(a.Location, radius)
				.Count(c => map.Contains(c) && terrainTypes.Contains(map.GetTerrainInfo(c).Type) &&
					Util.AdjacentCells(world, Target.FromCell(world, c))
						.All(ac => terrainTypes.Contains(map.GetTerrainInfo(ac).Type))))
							.Any(availableCells => availableCells > 0);
		}
	}
}
