#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.Render
{
	public class WithVoxelBodyInfo : ITraitInfo, Requires<RenderVoxelsInfo>
	{
		public object Create(ActorInitializer init) { return new WithVoxelBody(init.self); }
	}

	public class WithVoxelBody : IAutoSelectionSize
	{
		int2 size;

		public WithVoxelBody(Actor self)
		{
			var body = self.Trait<IBodyOrientation>();
			var rv = self.Trait<RenderVoxels>();

			var voxel = VoxelProvider.GetVoxel(rv.Image, "idle");
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
