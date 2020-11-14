#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class WithVoxelBarrelInfo : ConditionalTraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>, Requires<ArmamentInfo>, Requires<TurretedInfo>
	{
		[Desc("Voxel sequence name to use")]
		public readonly string Sequence = "barrel";

		[Desc("Armament to use for recoil")]
		public readonly string Armament = "primary";

		[Desc("Visual offset")]
		public readonly WVec LocalOffset = WVec.Zero;

		[Desc("Rotate the barrel relative to the body")]
		public readonly WRot LocalOrientation = WRot.None;

		[Desc("Defines if the Voxel should have a shadow.")]
		public readonly bool ShowShadow = true;

		public override object Create(ActorInitializer init) { return new WithVoxelBarrel(init.Self, this); }

		public IEnumerable<ModelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var armament = init.Actor.TraitInfos<ArmamentInfo>()
				.First(a => a.Name == Armament);
			var t = init.Actor.TraitInfos<TurretedInfo>()
				.First(tt => tt.Turret == armament.Turret);

			var model = init.World.ModelCache.GetModelSequence(image, Sequence);

			var turretOrientation = t.PreviewOrientation(init, orientation, facings);
			Func<WVec> barrelOffset = () => body.LocalToWorld(t.Offset + LocalOffset.Rotate(turretOrientation()));
			Func<WRot> barrelOrientation = () => LocalOrientation.Rotate(turretOrientation());

			yield return new ModelAnimation(model, barrelOffset, barrelOrientation, () => false, () => 0, ShowShadow);
		}
	}

	public class WithVoxelBarrel : ConditionalTrait<WithVoxelBarrelInfo>
	{
		readonly Actor self;
		readonly Armament armament;
		readonly Turreted turreted;
		readonly BodyOrientation body;

		public WithVoxelBarrel(Actor self, WithVoxelBarrelInfo info)
			: base(info)
		{
			this.self = self;
			body = self.Trait<BodyOrientation>();
			armament = self.TraitsImplementing<Armament>()
				.First(a => a.Info.Name == Info.Armament);
			turreted = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == armament.Info.Turret);

			var rv = self.Trait<RenderVoxels>();
			rv.Add(new ModelAnimation(self.World.ModelCache.GetModelSequence(rv.Image, Info.Sequence),
				BarrelOffset, BarrelRotation,
				() => IsTraitDisabled, () => 0, info.ShowShadow));
		}

		WVec BarrelOffset()
		{
			// Barrel offset in turret coordinates
			var localOffset = Info.LocalOffset + new WVec(-armament.Recoil, WDist.Zero, WDist.Zero);

			// Turret coordinates to body coordinates
			var bodyOrientation = body.QuantizeOrientation(self, self.Orientation);
			localOffset = localOffset.Rotate(turreted.WorldOrientation) + turreted.Offset.Rotate(bodyOrientation);

			// Body coordinates to world coordinates
			return body.LocalToWorld(localOffset);
		}

		WRot BarrelRotation()
		{
			return Info.LocalOrientation.Rotate(turreted.WorldOrientation);
		}
	}
}
