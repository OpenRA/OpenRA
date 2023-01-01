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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays an overlay whenever resources are harvested by the actor.")]
	class WithHarvestOverlayInfo : TraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "harvest";

		[Desc("Position relative to body")]
		public readonly WVec LocalOffset = WVec.Zero;

		[PaletteReference]
		public readonly string Palette = "effect";

		public override object Create(ActorInitializer init) { return new WithHarvestOverlay(init.Self, this); }
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

			anim = new Animation(self.World, rs.GetImage(self), RenderSprites.MakeFacingFunc(self))
			{
				IsDecoration = true
			};

			anim.Play(info.Sequence);
			rs.Add(new AnimationWithOffset(anim,
				() => body.LocalToWorld(info.LocalOffset.Rotate(body.QuantizeOrientation(self.Orientation))),
				() => !visible,
				p => ZOffsetFromCenter(self, p, 0)), info.Palette);
		}

		void INotifyHarvesterAction.Harvested(Actor self, string resourceType)
		{
			if (visible)
				return;

			visible = true;
			anim.PlayThen(info.Sequence, () => visible = false);
		}

		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell) { }
		void INotifyHarvesterAction.MovingToRefinery(Actor self, Actor targetRefinery) { }
		void INotifyHarvesterAction.MovementCancelled(Actor self) { }
		void INotifyHarvesterAction.Docked() { }
		void INotifyHarvesterAction.Undocked() { }

		public static int ZOffsetFromCenter(Actor self, WPos pos, int offset)
		{
			var delta = self.CenterPosition - pos;
			return delta.Y + delta.Z + offset;
		}
	}
}
