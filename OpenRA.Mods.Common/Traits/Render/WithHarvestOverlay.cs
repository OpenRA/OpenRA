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

using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays an overlay whenever resources are harvested by the actor.")]
	class WithHarvestOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "harvest";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference] public readonly string Palette = "effect";

		public object Create(ActorInitializer init) { return new WithHarvestOverlay(init.Self, this); }
	}

	class WithHarvestOverlay : INotifyHarvesterAction
	{
		readonly WithHarvestOverlayInfo info;
		readonly Animation anim;
		bool visible;

		public WithHarvestOverlay(Actor self, WithHarvestOverlayInfo info)
		{
			this.info = info;
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			anim = new Animation(self.World, rs.GetImage(self), RenderSprites.MakeFacingFunc(self));
			anim.IsDecoration = true;
			anim.Play(info.Sequence);
			rs.Add(new AnimationWithOffset(anim,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => !visible,
				p => ZOffsetFromCenter(self, p, 0)), info.Palette);
		}

		public void Harvested(Actor self, ResourceType resource)
		{
			if (visible)
				return;

			visible = true;
			anim.PlayThen(info.Sequence, () => visible = false);
		}

		public void MovingToResources(Actor self, CPos targetCell, Activity next) { }
		public void MovingToRefinery(Actor self, CPos targetCell, Activity next) { }
		public void MovementCancelled(Actor self) { }
		public void Docked() { }
		public void Undocked() { }

		public static int ZOffsetFromCenter(Actor self, WPos pos, int offset)
		{
			var delta = self.CenterPosition - pos;
			return delta.Y + delta.Z + offset;
		}
	}
}
