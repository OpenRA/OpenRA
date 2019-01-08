#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Mods.D2k.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Warheads
{
	[Desc("Interacts with the BuildableTerrainLayer trait.")]
	public class DamagesConcreteWarhead : Warhead
	{
		[Desc("How much damage to deal.")]
		[FieldLoader.Require]
		public readonly int Damage = 0;

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (target.Type == TargetType.Invalid)
				return;

			var world = firedBy.World;
			var layer = world.WorldActor.Trait<BuildableTerrainLayer>();
			var cell = world.Map.CellContaining(target.CenterPosition);
			layer.HitTile(cell, Damage);
		}
	}
}
