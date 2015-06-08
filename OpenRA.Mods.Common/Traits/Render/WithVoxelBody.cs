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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Also returns a default selection size that is calculated automatically from the voxel dimensions.")]
	public class WithVoxelBodyInfo : UpgradableTraitInfo, IQuantizeBodyOrientationInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>, Requires<IBodyOrientationInfo>
	{
		[SequenceReference] public readonly string Sequence = "idle";

		public override object Create(ActorInitializer init) { return new WithVoxelBody(init.Self, this); }

		public IEnumerable<VoxelAnimation> RenderPreviewVoxels(ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, WRot orientation, int facings, PaletteReference p)
		{
			if (UpgradeMinEnabledLevel > 0)
				yield break;

			var body = init.Actor.Traits.Get<BodyOrientationInfo>();
			var voxel = VoxelProvider.GetVoxel(image, Sequence);
			var bodyOrientation = new[] { body.QuantizeOrientation(orientation, facings) };
			yield return new VoxelAnimation(voxel, () => WVec.Zero,
				() => bodyOrientation,
				() => false, () => 0);
		}

		public int QuantizedBodyFacings(ActorInfo ai, SequenceProvider sequenceProvider, string race) { return 0; }
	}

	public class WithVoxelBody : UpgradableTrait<WithVoxelBodyInfo>, IAutoSelectionSize
	{
		readonly IBodyOrientation body;
		readonly RenderVoxels rv;
		int2 size;

		public WithVoxelBody(Actor self, WithVoxelBodyInfo info)
			: base(info)
		{
			body = self.Trait<IBodyOrientation>();
			rv = self.Trait<RenderVoxels>();

			var voxel = VoxelProvider.GetVoxel(rv.Image, Info.Sequence);
			rv.Add(new VoxelAnimation(voxel, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(self, self.Orientation) },
				() => IsTraitDisabled, () => 0));

			// Selection size
			var rvi = self.Info.Traits.Get<RenderVoxelsInfo>();
			var s = (int)(rvi.Scale * voxel.Size.Aggregate(Math.Max));
			size = new int2(s, s);
		}

		public int2 SelectionSize(Actor self) { return size; }
	}
}
