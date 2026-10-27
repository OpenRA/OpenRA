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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class MinelayerProperties : ScriptActorProperties, Requires<MinelayerInfo>
	{
		readonly World world;
		readonly Minelayer minelayer;

		public MinelayerProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			world = context.World;
			minelayer = self.Trait<Minelayer>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Lay mines along a set of given cells. Occupied cells risk being skipped. " +
		"Range is a maximum distance in cells that may offset each target cell.")]
		public void LayMines(CPos[] cells, int range = 0)
		{
			if (range > 0)
			{
				for (var i = 0; i < cells.Length; i++)
				{
					var candidateCells = world.Map.FindTilesInCircle(cells[i], range)
						.Where(c => minelayer.IsCellAcceptable(Self, c))
						.ToArray();

					if (candidateCells.Length == 0)
					{
						Log.Write("lua", $"{Self} found no good cells in range of {cells[i]}. Defaulted to center.");
						continue;
					}

					cells[i] = candidateCells.Random(world.SharedRandom);
				}
			}

			var minefield = new List<CPos>(cells);
			Self.QueueActivity(new LayMines(Self, minefield));
		}
	}
}
