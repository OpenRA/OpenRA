#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Render
{
	public class WithVoxelBarrelInfo : ITraitInfo, Requires<RenderVoxelsInfo>
	{
		[Desc("Voxel sequence name to use")]
		public readonly string Sequence = "barrel";
		[Desc("Armament to use for recoil")]
		public readonly string Armament = "primary";
		[Desc("Visual offset")]
		public readonly WVec LocalOffset = WVec.Zero;

		public object Create(ActorInitializer init) { return new WithVoxelBarrel(init.self, this); }
	}

	public class WithVoxelBarrel
	{
		WithVoxelBarrelInfo info;
		Actor self;
		Armament armament;
		Turreted turreted;
		IBodyOrientation body;

		public WithVoxelBarrel(Actor self, WithVoxelBarrelInfo info)
		{
			this.self = self;
			this.info = info;
			body = self.Trait<IBodyOrientation>();
			armament = self.TraitsImplementing<Armament>()
				.First(a => a.Info.Name == info.Armament);
			turreted = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == armament.Info.Turret);

			var rv = self.Trait<RenderVoxels>();
			rv.Add(new VoxelAnimation(VoxelProvider.GetVoxel(rv.Image, info.Sequence),
			                          () => BarrelOffset(), () => BarrelRotation(),
			                          () => false, () => 0));
		}

		WVec BarrelOffset()
		{
			var localOffset = info.LocalOffset + new WVec(-armament.Recoil, WRange.Zero, WRange.Zero);
			var turretOffset = turreted != null ? turreted.Position(self) : WVec.Zero;
			var turretOrientation = turreted != null ? turreted.LocalOrientation(self) : WRot.Zero;

			var quantizedBody = body.QuantizeOrientation(self, self.Orientation);
			var quantizedTurret =  body.QuantizeOrientation(self, turretOrientation);
			return turretOffset + body.LocalToWorld(localOffset.Rotate(quantizedTurret).Rotate(quantizedBody));
		}

		IEnumerable<WRot> BarrelRotation()
		{
			var b = self.Orientation;
			var qb = body.QuantizeOrientation(self, b);
			yield return turreted.LocalOrientation(self) + WRot.FromYaw(b.Yaw - qb.Yaw);
			yield return qb;
		}
	}
}
