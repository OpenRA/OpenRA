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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class PerCellDamageWarhead : DamageWarhead
	{
		[Desc("Size of the area. Damage will be applied to this area.")]
		public readonly int[] Size = { 0, 0 };

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);
			var minRange = (Size.Length > 1 && Size[1] > 0) ? Size[1] : 0;
			var affectedTiles = world.Map.FindTilesInAnnulus(targetTile, minRange, Size[0]);

			foreach (var t in affectedTiles)
				foreach (var victim in world.ActorMap.GetUnitsAt(t))
					DoImpact(victim, firedBy, damageModifiers);
		}
	}
}
