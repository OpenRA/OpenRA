#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Also returns a default selection size that is calculated automatically from the voxel dimensions.")]
	public class WithVoxelBodyInfo : ConditionalTraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>
	{
		public readonly string Sequence = "idle";

		[Desc("Defines if the Voxel should have a shadow.")]
		public readonly bool ShowShadow = true;

		public override object Create(ActorInitializer init) { return new WithVoxelBody(init.Self, this); }

		public IEnumerable<VoxelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var voxel = VoxelProvider.GetVoxel(image, Sequence);
			yield return new VoxelAnimation(voxel, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(orientation(), facings) },
				() => false, () => 0, ShowShadow);
		}
	}

	public class WithVoxelBody : ConditionalTrait<WithVoxelBodyInfo>, IAutoSelectionSize
	{
		readonly int2 size;

		public WithVoxelBody(Actor self, WithVoxelBodyInfo info)
			: base(info)
		{
			var body = self.Trait<BodyOrientation>();
			var rv = self.Trait<RenderVoxels>();

			var voxel = VoxelProvider.GetVoxel(rv.Image, info.Sequence);
			rv.Add(new VoxelAnimation(voxel, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(self, self.Orientation) },
				() => IsTraitDisabled, () => 0, info.ShowShadow));

			// Selection size
			var rvi = self.Info.TraitInfo<RenderVoxelsInfo>();
			var s = (int)(rvi.Scale * voxel.Size.Aggregate(Math.Max));
			size = new int2(s, s);
		}

		public int2 SelectionSize(Actor self) { return size; }
	}
}
