#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	// TODO: Add functionality like a customizable Height that is compared to projectile altitude
	[Desc("This actor blocks bullets and missiles with 'Blockable' property.")]
	public class BlocksProjectilesInfo : UpgradableTraitInfo
	{
		public override object Create(ActorInitializer init) { return new BlocksProjectiles(init.Self, this); }
	}

	public class BlocksProjectiles : UpgradableTrait<BlocksProjectilesInfo>
	{
		public BlocksProjectiles(Actor self, BlocksProjectilesInfo info)
			: base(info) { }

		public static bool AnyBlockingActorAt(World world, WPos pos)
		{
			return world.ActorMap.GetActorsAt(world.Map.CellContaining(pos))
				.Any(a => a.TraitsImplementing<BlocksProjectiles>().Any(Exts.IsTraitEnabled));
		}
	}
}
