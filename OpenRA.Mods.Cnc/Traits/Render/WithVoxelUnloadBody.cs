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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	// TODO: This trait is hacky and should go away as soon as we support granting a condition on docking, in favor of toggling two regular WithVoxelBodies
	public class WithVoxelUnloadBodyInfo : TraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>
	{
		[Desc("Voxel sequence name to use when docked to a refinery.")]
		public readonly string UnloadSequence = "unload";

		[Desc("Voxel sequence name to use when undocked from a refinery.")]
		public readonly string IdleSequence = "idle";

		[Desc("Defines if the Voxel should have a shadow.")]
		public readonly bool ShowShadow = true;

		public override object Create(ActorInitializer init) { return new WithVoxelUnloadBody(init.Self, this); }

		public IEnumerable<ModelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var model = init.World.ModelCache.GetModelSequence(image, IdleSequence);
			yield return new ModelAnimation(model, () => WVec.Zero,
				() => body.QuantizeOrientation(orientation(), facings),
				() => false, () => 0, ShowShadow);
		}
	}

	public class WithVoxelUnloadBody : IAutoMouseBounds, IDockClientBody
	{
		bool docked;

		readonly ModelAnimation modelAnimation;
		readonly RenderVoxels rv;

		public WithVoxelUnloadBody(Actor self, WithVoxelUnloadBodyInfo info)
		{
			var body = self.Trait<BodyOrientation>();
			rv = self.Trait<RenderVoxels>();

			var idleModel = self.World.ModelCache.GetModelSequence(rv.Image, info.IdleSequence);
			modelAnimation = new ModelAnimation(idleModel, () => WVec.Zero,
				() => body.QuantizeOrientation(self.Orientation),
				() => docked,
				() => 0, info.ShowShadow);

			rv.Add(modelAnimation);

			var unloadModel = self.World.ModelCache.GetModelSequence(rv.Image, info.UnloadSequence);
			rv.Add(new ModelAnimation(unloadModel, () => WVec.Zero,
				() => body.QuantizeOrientation(self.Orientation),
				() => !docked,
				() => 0, info.ShowShadow));
		}

		void IDockClientBody.PlayDockAnimation(Actor self, Action after)
		{
			docked = true;
			after();
		}

		void IDockClientBody.PlayReverseDockAnimation(Actor self, Action after)
		{
			docked = false;
			after();
		}

		Rectangle IAutoMouseBounds.AutoMouseoverBounds(Actor self, WorldRenderer wr)
		{
			return modelAnimation.ScreenBounds(self.CenterPosition, wr, rv.Info.Scale);
		}
	}
}
