#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
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

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (target.Type == TargetType.Invalid)
				return;

			var firedBy = args.SourceActor;
			var world = firedBy.World;
			var layer = world.WorldActor.Trait<BuildableTerrainLayer>();
			var cell = world.Map.CellContaining(target.CenterPosition);
			layer.HitTile(cell, Damage);
		}
	}
}
