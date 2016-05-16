#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[RequireExplicitImplementation]
	public interface IBlocksProjectiles
	{
		WDist BlockingHeight { get; }
	}

	[Desc("This actor blocks bullets and missiles with 'Blockable' property.")]
	public class BlocksProjectilesInfo : UpgradableTraitInfo
	{
		public readonly WDist Height = WDist.FromCells(1);

		public override object Create(ActorInitializer init) { return new BlocksProjectiles(init.Self, this); }
	}

	public class BlocksProjectiles : UpgradableTrait<BlocksProjectilesInfo>, IBlocksProjectiles
	{
		public BlocksProjectiles(Actor self, BlocksProjectilesInfo info)
			: base(info) { }

		WDist IBlocksProjectiles.BlockingHeight { get { return Info.Height; } }

		public static bool AnyBlockingActorAt(World world, WPos pos)
		{
			var dat = world.Map.DistanceAboveTerrain(pos);

			return world.ActorMap.GetActorsAt(world.Map.CellContaining(pos))
				.Any(a => a.TraitsImplementing<IBlocksProjectiles>()
					.Where(t => t.BlockingHeight > dat)
					.Any(Exts.IsTraitEnabled));
		}

		public static bool AnyBlockingActorsBetween(World world, WPos source, WPos start, WPos end, WDist width, WDist overscan, out WPos hit)
		{
			var actors = world.FindActorsOnLine(start, end, width, overscan);
			var length = (end - start).Length;

			foreach (var a in actors)
			{
				var blockers = a.TraitsImplementing<IBlocksProjectiles>()
					.Where(Exts.IsTraitEnabled).ToList();

				if (!blockers.Any())
					continue;

				// FindActorsOnLine only finds actors with health trait, so this actor is guaranteed to have it
				var healthInfo = a.Info.TraitInfo<HealthInfo>();
				var vector = (a.CenterPosition - source) * healthInfo.Shape.InnerRadius.Length / (a.CenterPosition - source).Length;
				var hitShapeEdgePos = a.CenterPosition - vector;
				var hitPos = WorldExtensions.MinimumPointLineProjection(source, a.CenterPosition, hitShapeEdgePos);

				var dat = world.Map.DistanceAboveTerrain(hitPos);
				if ((hitPos - start).Length < length && blockers.Any(t => t.BlockingHeight > dat))
				{
					hit = hitPos;
					return true;
				}
			}

			hit = WPos.Zero;
			return false;
		}
	}
}
