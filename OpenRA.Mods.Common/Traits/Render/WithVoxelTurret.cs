#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.Traits
{
	public class WithVoxelTurretInfo : UpgradableTraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>, Requires<TurretedInfo>
	{
		[Desc("Voxel sequence name to use")]
		public readonly string Sequence = "turret";

		[Desc("Turreted 'Turret' key to display")]
		public readonly string Turret = "primary";

		public override object Create(ActorInitializer init) { return new WithVoxelTurret(init.Self, this); }

		public IEnumerable<VoxelAnimation> RenderPreviewVoxels(ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, WRot orientation, int facings, PaletteReference p)
		{
			if (UpgradeMinEnabledLevel > 0)
				yield break;

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var t = init.Actor.TraitInfos<TurretedInfo>()
				.First(tt => tt.Turret == Turret);

			var voxel = VoxelProvider.GetVoxel(image, Sequence);
			var turretOffset = body.LocalToWorld(t.Offset.Rotate(orientation));

			var turretFacing = Turreted.GetInitialTurretFacing(init, t.InitialFacing, Turret);
			var turretBodyOrientation = new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(turretFacing) - orientation.Yaw);
			var turretOrientation = new[] { turretBodyOrientation, body.QuantizeOrientation(orientation, facings) };
			yield return new VoxelAnimation(voxel, () => turretOffset, () => turretOrientation, () => false, () => 0);
		}
	}

	public class WithVoxelTurret : UpgradableTrait<WithVoxelTurretInfo>
	{
		readonly Actor self;
		readonly Turreted turreted;
		readonly BodyOrientation body;

		public WithVoxelTurret(Actor self, WithVoxelTurretInfo info)
			: base(info)
		{
			this.self = self;
			body = self.Trait<BodyOrientation>();
			turreted = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == Info.Turret);

			var rv = self.Trait<RenderVoxels>();
			rv.Add(new VoxelAnimation(VoxelProvider.GetVoxel(rv.Image, Info.Sequence),
				() => turreted.Position(self), TurretRotation,
				() => IsTraitDisabled, () => 0));
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
