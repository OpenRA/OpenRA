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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits.Render
{
	public class WithVoxelWaterBodyInfo : ITraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>
	{
		public readonly string WaterSequence = "water";
		public readonly string LandSequence = "idle";

		public object Create(ActorInitializer init) { return new WithVoxelWaterBody(init.Self, this); }

		public IEnumerable<VoxelAnimation> RenderPreviewVoxels(ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, WRot orientation, int facings, PaletteReference p)
		{
			var sequence = LandSequence;
			if (init.Contains<LocationInit>())
			{
				var location = init.Get<LocationInit>().Value(init.World);
				var onWater = init.World.Map.GetTerrainInfo(location).IsWater;
				sequence = onWater ? WaterSequence : LandSequence;
			}

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var voxel = VoxelProvider.GetVoxel(image, sequence);
			yield return new VoxelAnimation(voxel, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(orientation, facings) },
				() => false, () => 0);
		}
	}

	public class WithVoxelWaterBody : IAutoSelectionSize
	{
		readonly Actor self;
		readonly int2 size;

		bool OverWater { get { return self.World.Map.GetTerrainInfo(self.Location).IsWater; } }

		public WithVoxelWaterBody(Actor self, WithVoxelWaterBodyInfo info)
		{
			this.self = self;

			var body = self.Trait<BodyOrientation>();
			var rv = self.Trait<RenderVoxels>();

			var landVoxel = VoxelProvider.GetVoxel(rv.Image, info.LandSequence);
			rv.Add(new VoxelAnimation(landVoxel, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(self, self.Orientation) },
				() => OverWater,
				() => 0));

			// Selection size
			var rvi = self.Info.TraitInfo<RenderVoxelsInfo>();
			var s = (int)(rvi.Scale * landVoxel.Size.Aggregate(Math.Max));
			size = new int2(s, s);

			var waterVoxel = VoxelProvider.GetVoxel(rv.Image, info.WaterSequence);
			rv.Add(new VoxelAnimation(waterVoxel, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(self, self.Orientation) },
				() => !OverWater,
				() => 0));
		}

		public int2 SelectionSize(Actor self) { return size; }
	}
}
