#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	public class WithVoxelWalkerBodyInfo : ITraitInfo, IRenderActorPreviewVoxelsInfo,  Requires<RenderVoxelsInfo>, Requires<IMoveInfo>, Requires<IFacingInfo>
	{
		public readonly string Sequence = "idle";

		[Desc("The speed of the walker's legs.")]
		public readonly int TickRate = 5;

		[Desc("Defines if the Voxel should have a shadow.")]
		public readonly bool ShowShadow = true;
		public object Create(ActorInitializer init) { return new WithVoxelWalkerBody(init.Self, this); }

		public IEnumerable<ModelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			var model = init.World.ModelCache.GetModelSequence(image, Sequence);
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var frame = init.Contains<BodyAnimationFrameInit>() ? init.Get<BodyAnimationFrameInit, uint>() : 0;

			yield return new ModelAnimation(model, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(orientation(), facings) },
				() => false, () => frame, ShowShadow);
		}
	}

	public class WithVoxelWalkerBody : ITick, IActorPreviewInitModifier, IAutoMouseBounds
	{
		readonly WithVoxelWalkerBodyInfo info;
		readonly IMove movement;
		readonly ModelAnimation modelAnimation;
		readonly RenderVoxels rv;

		uint tick, frame, frames;

		public WithVoxelWalkerBody(Actor self, WithVoxelWalkerBodyInfo info)
		{
			this.info = info;
			movement = self.Trait<IMove>();

			var body = self.Trait<BodyOrientation>();
			rv = self.Trait<RenderVoxels>();

			var model = self.World.ModelCache.GetModelSequence(rv.Image, info.Sequence);
			frames = model.Frames;
			modelAnimation = new ModelAnimation(model, () => WVec.Zero,
				() => new[] { body.QuantizeOrientation(self, self.Orientation) },
				() => false, () => frame, info.ShowShadow);

			rv.Add(modelAnimation);
		}

		void ITick.Tick(Actor self)
		{
			if (movement.IsMoving)
				tick++;

			if (tick < info.TickRate)
				return;

			tick = 0;
			if (++frame == frames)
				frame = 0;
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			inits.Add(new BodyAnimationFrameInit(frame));
		}

		Rectangle IAutoMouseBounds.AutoMouseoverBounds(Actor self, WorldRenderer wr)
		{
			return modelAnimation.ScreenBounds(self.CenterPosition, wr, rv.Info.Scale);
		}
	}

	public class BodyAnimationFrameInit : IActorInit<uint>
	{
		[FieldFromYamlKey] readonly uint value = 0;
		public BodyAnimationFrameInit() { }
		public BodyAnimationFrameInit(uint init) { value = init; }
		public uint Value(World world) { return value; }
	}
}
