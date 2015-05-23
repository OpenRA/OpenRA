#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits
{
	[Desc("Also returns a default selection size that is calculated automatically from the voxel dimensions.")]
	public class WithVoxelBodyInfo : ITraitInfo, IQuantizeBodyOrientationInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>
	{
		public readonly string Sequence = "idle";

		public object Create(ActorInitializer init) { return new WithVoxelBody(init.self, this); }

		public IEnumerable<VoxelAnimation> RenderPreviewVoxels(ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, WRot orientation, int facings, PaletteReference p)
		{
			var body = init.Actor.Traits.Get<BodyOrientationInfo>();
			var voxel = VoxelProvider.GetVoxel(image, "idle");
			var bodyOrientation = new[] { body.QuantizeOrientation(orientation, facings) };
			yield return new VoxelAnimation(voxel, () => WVec.Zero,
				() => bodyOrientation,
				() => false, () => 0);
		}

		public int QuantizedBodyFacings(SequenceProvider sequenceProvider, ActorInfo ai) { return 0; }
	}

	public class WithVoxelBody : IAutoSelectionSize
	{
		int2 size;

		public WithVoxelBody(Actor self, WithVoxelBodyInfo info)
		{
			var body = self.Trait<IBodyOrientation>();
			var rv = self.Trait<RenderVoxels>();

			var voxel = VoxelProvider.GetVoxel(rv.Image, info.Sequence);
			rv.Add(new VoxelAnimation(voxel, () => WVec.Zero,
			                           () => new[]{ body.QuantizeOrientation(self, self.Orientation) },
			                           () => false, () => 0));

			// Selection size
			var rvi = self.Info.Traits.Get<RenderVoxelsInfo>();
			var s = (int)(rvi.Scale*voxel.Size.Aggregate(Math.Max));
			size = new int2(s, s);
		}

		public int2 SelectionSize(Actor self) { return size; }
	}
}
