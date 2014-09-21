#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class WithVoxelTurretInfo : ITraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>, Requires<TurretedInfo>
	{
		[Desc("Voxel sequence name to use")]
		public readonly string Sequence = "turret";

		[Desc("Turreted 'Turret' key to display")]
		public readonly string Turret = "primary";

		public object Create(ActorInitializer init) { return new WithVoxelTurret(init.self, this); }

		public IEnumerable<VoxelAnimation> RenderPreviewVoxels(ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, WRot orientation, int facings, PaletteReference p)
		{
			var body = init.Actor.Traits.Get<BodyOrientationInfo>();
			var t = init.Actor.Traits.WithInterface<TurretedInfo>()
				.First(tt => tt.Turret == Turret);

			var voxel = VoxelProvider.GetVoxel(image, Sequence);
			var turretOffset = body.LocalToWorld(t.Offset.Rotate(orientation));

			var turretBodyOrientation = new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(t.InitialFacing) - orientation.Yaw);
			var turretOrientation = new[] { turretBodyOrientation, body.QuantizeOrientation(orientation, facings) };
			yield return new VoxelAnimation(voxel, () => turretOffset, () => turretOrientation, () => false, () => 0);
		}
	}

	public class WithVoxelTurret
	{
		Actor self;
		Turreted turreted;
		IBodyOrientation body;

		public WithVoxelTurret(Actor self, WithVoxelTurretInfo info)
		{
			this.self = self;
			body = self.Trait<IBodyOrientation>();
			turreted = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == info.Turret);

			var rv = self.Trait<RenderVoxels>();
			rv.Add(new VoxelAnimation(VoxelProvider.GetVoxel(rv.Image, info.Sequence),
			                          () => turreted.Position(self), () => TurretRotation(),
			                          () => false, () => 0));
		}

		IEnumerable<WRot> TurretRotation()
		{
			var b = self.Orientation;
			var qb = body.QuantizeOrientation(self, b);
			yield return turreted.LocalOrientation(self) + WRot.FromYaw(b.Yaw - qb.Yaw);
			yield return qb;
		}
	}
}
