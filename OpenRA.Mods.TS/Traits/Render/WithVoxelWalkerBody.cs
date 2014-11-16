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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class WithVoxelWalkerBodyInfo : ITraitInfo, IQuantizeBodyOrientationInfo, Requires<RenderVoxelsInfo>, Requires<IMoveInfo>
	{
		public readonly int TickRate = 5;
		public object Create(ActorInitializer init) { return new WithVoxelWalkerBody(init.self, this); }

		public int QuantizedBodyFacings(SequenceProvider sequenceProvider, ActorInfo ai) { return 0; }
	}

	public class WithVoxelWalkerBody : IAutoSelectionSize, ITick
	{
		WithVoxelWalkerBodyInfo info;
		IMove movement;
		int2 size;
		uint tick, frame, frames;

		public WithVoxelWalkerBody(Actor self, WithVoxelWalkerBodyInfo info)
		{
			this.info = info;
			movement = self.Trait<IMove>();

			var body = self.Trait<IBodyOrientation>();
			var rv = self.Trait<RenderVoxels>();

			var voxel = VoxelProvider.GetVoxel(rv.Image, "idle");
			frames = voxel.Frames;
			rv.Add(new VoxelAnimation(voxel, () => WVec.Zero,
			                           () => new[]{ body.QuantizeOrientation(self, self.Orientation) },
			                           () => false, () => frame));

			// Selection size
			var rvi = self.Info.Traits.Get<RenderVoxelsInfo>();
			var s = (int)(rvi.Scale*voxel.Size.Aggregate(Math.Max));
			size = new int2(s, s);
		}

		public int2 SelectionSize(Actor self) { return size; }

		public void Tick(Actor self)
		{
			if (movement.IsMoving)
				tick++;

			if (tick < info.TickRate)
				return;

			tick = 0;
			if (++frame == frames)
				frame = 0;
		}
	}
}
