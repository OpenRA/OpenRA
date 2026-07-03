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

namespace OpenRA.Mods.Common.Orders
{
	public static class FormationPreview
	{
		public static IEnumerable<CPos> GetDestinationOccupiedCells(World world, Actor[] actors, CPos anchorCell, FormationType formation)
		{
			var destinations = FormationResolver.AssignDestinations(world, actors, anchorCell, formation);
			foreach (var actor in actors)
			{
				if (!destinations.TryGetValue(actor, out var dest))
					continue;

				var occupied = actor.OccupiesSpace?.OccupiedCells().ToArray() ?? [];
				if (occupied.Length == 0)
				{
					yield return dest;
					continue;
				}

				var delta = dest - actor.Location;
				foreach (var p in occupied)
					yield return p.Cell + delta;
			}
		}
	}
}
