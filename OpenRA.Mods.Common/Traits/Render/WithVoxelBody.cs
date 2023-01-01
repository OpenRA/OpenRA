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

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
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

		public IEnumerable<ModelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var model = init.World.ModelCache.GetModelSequence(image, Sequence);
			yield return new ModelAnimation(model, () => WVec.Zero,
				() => body.QuantizeOrientation(orientation(), facings),
				() => false, () => 0, ShowShadow);
		}
	}

	public class WithVoxelBody : ConditionalTrait<WithVoxelBodyInfo>, IAutoMouseBounds
	{
		readonly ModelAnimation modelAnimation;
		readonly RenderVoxels rv;

		public WithVoxelBody(Actor self, WithVoxelBodyInfo info)
			: base(info)
		{
			var body = self.Trait<BodyOrientation>();
			rv = self.Trait<RenderVoxels>();

			var model = self.World.ModelCache.GetModelSequence(rv.Image, info.Sequence);
			modelAnimation = new ModelAnimation(model, () => WVec.Zero,
				() => body.QuantizeOrientation(self.Orientation),
				() => IsTraitDisabled, () => 0, info.ShowShadow);

			rv.Add(modelAnimation);
		}

		Rectangle IAutoMouseBounds.AutoMouseoverBounds(Actor self, WorldRenderer wr)
		{
			return modelAnimation.ScreenBounds(self.CenterPosition, wr, rv.Info.Scale);
		}
	}
}
