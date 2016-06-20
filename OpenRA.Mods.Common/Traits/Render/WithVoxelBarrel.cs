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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithVoxelBarrelInfo : UpgradableTraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>, Requires<ArmamentInfo>, Requires<TurretedInfo>
	{
		[Desc("Voxel sequence name to use")]
		public readonly string Sequence = "barrel";

		[Desc("Armament to use for recoil")]
		public readonly string Armament = "primary";

		[Desc("Visual offset")]
		public readonly WVec LocalOffset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new WithVoxelBarrel(init.Self, this); }

		public IEnumerable<VoxelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			if (UpgradeMinEnabledLevel > 0)
				yield break;

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var armament = init.Actor.TraitInfos<ArmamentInfo>()
				.First(a => a.Name == Armament);
			var t = init.Actor.TraitInfos<TurretedInfo>()
				.First(tt => tt.Turret == armament.Turret);

			var voxel = VoxelProvider.GetVoxel(image, Sequence);

			var turretFacing = Turreted.TurretFacingFromInit(init, t.InitialFacing, t.Turret);
			Func<WRot> turretOrientation = () => body.QuantizeOrientation(WRot.FromYaw(WAngle.FromFacing(turretFacing()) - orientation().Yaw), facings);

			Func<WRot> quantizedTurret = () => body.QuantizeOrientation(turretOrientation(), facings);
			Func<WRot> quantizedBody = () => body.QuantizeOrientation(orientation(), facings);
			Func<WVec> barrelOffset = () => body.LocalToWorld((t.Offset + LocalOffset.Rotate(quantizedTurret())).Rotate(quantizedBody()));

			yield return new VoxelAnimation(voxel, barrelOffset, () => new[] { turretOrientation(), orientation() },
				() => false, () => 0);
		}
	}

	public class WithVoxelBarrel : UpgradableTrait<WithVoxelBarrelInfo>, INotifyBuildComplete, INotifySold, INotifyTransform
	{
		readonly Actor self;
		readonly Armament armament;
		readonly Turreted turreted;
		readonly BodyOrientation body;

		// TODO: This should go away once https://github.com/OpenRA/OpenRA/issues/7035 is implemented
		bool buildComplete;

		public WithVoxelBarrel(Actor self, WithVoxelBarrelInfo info)
			: base(info)
		{
			this.self = self;
			body = self.Trait<BodyOrientation>();
			armament = self.TraitsImplementing<Armament>()
				.First(a => a.Info.Name == Info.Armament);
			turreted = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == armament.Info.Turret);

			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units

			var rv = self.Trait<RenderVoxels>();
			rv.Add(new VoxelAnimation(VoxelProvider.GetVoxel(rv.Image, Info.Sequence),
				BarrelOffset, BarrelRotation,
				() => IsTraitDisabled || !buildComplete, () => 0));
		}

		WVec BarrelOffset()
		{
			var b = self.Orientation;
			var qb = body.QuantizeOrientation(self, b);
			var localOffset = Info.LocalOffset + new WVec(-armament.Recoil, WDist.Zero, WDist.Zero);
			var turretLocalOffset = turreted != null ? turreted.Offset : WVec.Zero;
			var turretOrientation = turreted != null ? turreted.WorldOrientation(self) - b + WRot.FromYaw(b.Yaw - qb.Yaw) : WRot.Zero;

			return body.LocalToWorld((turretLocalOffset + localOffset.Rotate(turretOrientation)).Rotate(qb));
		}

		IEnumerable<WRot> BarrelRotation()
		{
			var b = self.Orientation;
			var qb = body.QuantizeOrientation(self, b);
			yield return turreted.WorldOrientation(self) - b + WRot.FromYaw(b.Yaw - qb.Yaw);
			yield return qb;
		}

		void INotifyBuildComplete.BuildingComplete(Actor self) { buildComplete = true; }
		void INotifySold.Selling(Actor self) { buildComplete = false; }
		void INotifySold.Sold(Actor self) { }
		void INotifyTransform.BeforeTransform(Actor self) { buildComplete = false; }
		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor toActor) { }
	}
}
