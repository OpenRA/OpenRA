#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class WithVoxelTurretInfo : ConditionalTraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>, Requires<TurretedInfo>
	{
		[Desc("Voxel sequence name to use")]
		public readonly string Sequence = "turret";

		[Desc("Turreted 'Turret' key to display")]
		public readonly string Turret = "primary";

		[Desc("Defines if the Voxel should have a shadow.")]
		public readonly bool ShowShadow = true;

		public override object Create(ActorInitializer init) { return new WithVoxelTurret(init.Self, this); }

		public IEnumerable<ModelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var t = init.Actor.TraitInfos<TurretedInfo>()
				.First(tt => tt.Turret == Turret);

			var model = init.World.ModelCache.GetModelSequence(image, Sequence);
			var turretOffset = t.PreviewPosition(init, orientation);
			var turretOrientation = t.PreviewOrientation(init, orientation, facings);
			yield return new ModelAnimation(model, turretOffset, turretOrientation, () => false, () => 0, ShowShadow);
		}
	}

	public class WithVoxelTurret : ConditionalTrait<WithVoxelTurretInfo>
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
			rv.Add(new ModelAnimation(self.World.ModelCache.GetModelSequence(rv.Image, Info.Sequence),
				() => turreted.Position(self), () => turreted.WorldOrientation,
				() => IsTraitDisabled, () => 0, info.ShowShadow));
		}
	}
}
