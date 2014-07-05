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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class WithVoxelUnloadBodyInfo : ITraitInfo, Requires<RenderVoxelsInfo>
	{
		[Desc("Voxel sequence name to use when docked to a refinery.")]
		public readonly string UnloadSequence = "unload";

		[Desc("Voxel sequence name to use when undocked from a refinery.")]
		public readonly string IdleSequence = "idle";

		public object Create(ActorInitializer init) { return new WithVoxelUnloadBody(init.self, this); }
	}

	public class WithVoxelUnloadBody : IAutoSelectionSize
	{
		public bool Docked;

		readonly int2 size;

		public WithVoxelUnloadBody(Actor self, WithVoxelUnloadBodyInfo info)
		{
			var body = self.Trait<IBodyOrientation>();
			var rv = self.Trait<RenderVoxels>();

			var idleVoxel = VoxelProvider.GetVoxel(rv.Image, info.IdleSequence);
			rv.Add(new VoxelAnimation(idleVoxel, () => WVec.Zero,
				() => new[]{ body.QuantizeOrientation(self, self.Orientation) },
				() => Docked,
				() => 0));

			// Selection size
			var rvi = self.Info.Traits.Get<RenderVoxelsInfo>();
			var s = (int)(rvi.Scale * idleVoxel.Size.Aggregate(Math.Max));
			size = new int2(s, s);

			var unloadVoxel = VoxelProvider.GetVoxel(rv.Image, info.UnloadSequence);
			rv.Add(new VoxelAnimation(unloadVoxel, () => WVec.Zero,
				() => new[]{ body.QuantizeOrientation(self, self.Orientation) },
				() => !Docked,
				() => 0));
		}

		public int2 SelectionSize(Actor self) { return size; }
	}
}
