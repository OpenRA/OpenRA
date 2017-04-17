#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Support;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Render an animated voxel.")]
	public class WithVoxelAnimatedBodyInfo : ConditionalTraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>
	{
		public readonly string Sequence = "idle";

		[Desc("The rate of the voxel animation.")]
		public readonly int TickRate = 5;

		[Desc("Defines if the Voxel should have a shadow.")]
		public readonly bool ShowShadow = true;

		public override object Create(ActorInitializer init) { return new WithVoxelAnimatedBody(init.Self, this); }

		public IEnumerable<VoxelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			var voxel = VoxelProvider.GetVoxel(image, Sequence);
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var frame = init.Contains<BodyAnimationFrameInit>() ? init.Get<BodyAnimationFrameInit, uint>() : 0;

			yield return new VoxelAnimation(voxel, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(orientation(), facings) },
				() => false, () => frame, ShowShadow);
		}
	}

	public class WithVoxelAnimatedBody : ConditionalTrait<WithVoxelAnimatedBodyInfo>, IAutoSelectionSize, ITick, IActorPreviewInitModifier
	{
		WithVoxelAnimatedBodyInfo info;
		int2 size;
		uint tick, frame, frames;

		public WithVoxelAnimatedBody(Actor self, WithVoxelAnimatedBodyInfo info)
			: base(info)
		{
			this.info = info;

			var body = self.Trait<BodyOrientation>();
			var rv = self.Trait<RenderVoxels>();

			var voxel = VoxelProvider.GetVoxel(rv.Image, info.Sequence);
			frames = voxel.Frames;
			rv.Add(new VoxelAnimation(voxel, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(self, self.Orientation) },
				() => IsTraitDisabled, () => frame, info.ShowShadow));

			// Selection size
			var rvi = self.Info.TraitInfo<RenderVoxelsInfo>();
			var s = (int)(rvi.Scale * voxel.Size.Aggregate(Math.Max));
			size = new int2(s, s);
		}

		public int2 SelectionSize(Actor self) { return size; }

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			tick++;

			if (tick < info.TickRate)
				return;

			tick = 0;
			if (++frame == frames)
				frame = 0;
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			inits.Add(new BodyAnimationFrameInit(frame));
		}
	}
}
