#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	public class WithModelBarrelInfo : ConditionalTraitInfo, IRenderActorPreviewModelsInfo, Requires<RenderModelsInfo>, Requires<ArmamentInfo>, Requires<TurretedInfo>
	{
		[Desc("Model sequence name to use")]
		public readonly string Sequence = "barrel";

		[Desc("Armament to use for recoil")]
		public readonly string Armament = "primary";

		[Desc("Visual offset")]
		public readonly WVec LocalOffset = WVec.Zero;

		[Desc("Rotate the barrel relative to the body")]
		public readonly WRot LocalOrientation = WRot.None;

		[Desc("Defines if the Model should have a shadow.")]
		public readonly bool ShowShadow = true;

		public override object Create(ActorInitializer init) { return new WithModelBarrel(init.Self, this); }

		public IEnumerable<ModelAnimation> RenderPreviewModels(
			ActorPreviewInitializer init, RenderModelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
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
			WVec BarrelOffset() => body.LocalToWorld(t.Offset + LocalOffset.Rotate(turretOrientation()));
			WRot BarrelOrientation() => LocalOrientation.Rotate(turretOrientation());

			yield return new ModelAnimation(model, BarrelOffset, BarrelOrientation, () => false, () => 0, ShowShadow);
		}
	}

	public class WithModelBarrel : ConditionalTrait<WithModelBarrelInfo>
	{
		readonly Actor self;
		readonly Armament armament;
		readonly Turreted turreted;
		readonly BodyOrientation body;

		public WithModelBarrel(Actor self, WithModelBarrelInfo info)
			: base(info)
		{
			this.self = self;
			body = self.Trait<BodyOrientation>();
			armament = self.TraitsImplementing<Armament>()
				.First(a => a.Info.Name == Info.Armament);
			turreted = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == armament.Info.Turret);

			var rv = self.Trait<RenderModels>();
			rv.Add(new ModelAnimation(self.World.ModelCache.GetModelSequence(rv.Image, Info.Sequence),
				BarrelOffset, BarrelRotation,
				() => IsTraitDisabled, () => 0, info.ShowShadow));
		}

		WVec BarrelOffset()
		{
			// Barrel offset in turret coordinates
			var localOffset = Info.LocalOffset + new WVec(-armament.Recoil, WDist.Zero, WDist.Zero);

			// Turret coordinates to body coordinates
			var bodyOrientation = body.QuantizeOrientation(self.Orientation);
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
