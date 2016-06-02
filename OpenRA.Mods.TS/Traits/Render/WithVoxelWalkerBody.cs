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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits.Render
{
	public class WithVoxelWalkerBodyInfo : ITraitInfo, Requires<RenderVoxelsInfo>, Requires<IMoveInfo>, Requires<IFacingInfo>
	{
		public readonly int TickRate = 5;
		public object Create(ActorInitializer init) { return new WithVoxelWalkerBody(init.Self, this); }
	}

	public class WithVoxelWalkerBody : IAutoSelectionSize, ITick
	{
		WithVoxelWalkerBodyInfo info;
		IMove movement;
		IFacing facing;
		int oldFacing;
		int2 size;
		uint tick, frame, frames;

		public WithVoxelWalkerBody(Actor self, WithVoxelWalkerBodyInfo info)
		{
			this.info = info;
			movement = self.Trait<IMove>();
			facing = self.Trait<IFacing>();

			var body = self.Trait<BodyOrientation>();
			var rv = self.Trait<RenderVoxels>();

			var voxel = VoxelProvider.GetVoxel(rv.Image, "idle");
			frames = voxel.Frames;
			rv.Add(new VoxelAnimation(voxel, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(self, self.Orientation) },
				() => false, () => frame));

			// Selection size
			var rvi = self.Info.TraitInfo<RenderVoxelsInfo>();
			var s = (int)(rvi.Scale * voxel.Size.Aggregate(Math.Max));
			size = new int2(s, s);
		}

		public int2 SelectionSize(Actor self) { return size; }

		public void Tick(Actor self)
		{
			if (movement.IsMoving || facing.Facing != oldFacing)
				tick++;
			oldFacing = facing.Facing;

			if (tick < info.TickRate)
				return;

			tick = 0;
			if (++frame == frames)
				frame = 0;
		}
	}
}
