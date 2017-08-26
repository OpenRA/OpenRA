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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders a looping animation over units and buildings between random intervals.")]
	public class WithRandomIntervalOverlayInfo : PausableConditionalTraitInfo
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "idle-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Minimum time to wait.")]
		[FieldLoader.Require] public readonly int MinRandomWaitTicks = 30;

		[Desc("Maximum time to wait.")]
		[FieldLoader.Require] public readonly int MaxRandomWaitTicks = 110;

		public override object Create(ActorInitializer init) { return new WithRandomIntervalOverlay(init.Self, this); }
	}

	public class WithRandomIntervalOverlay : PausableConditionalTrait<WithRandomIntervalOverlayInfo>, INotifyDamageStateChanged,
		INotifyBuildComplete, INotifySold, INotifyTransform, ITick
	{
		readonly Animation overlay;
		readonly WithRandomIntervalOverlayInfo info;
		bool buildComplete;
		int randomDelay;

		public WithRandomIntervalOverlay(Actor self, WithRandomIntervalOverlayInfo info)
			: base(info)
		{
			this.info = info;
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units
			overlay = new Animation(self.World, rs.GetImage(self), () => IsTraitPaused || !buildComplete);

			LoopAnimation(self);

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => IsTraitDisabled || !buildComplete,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		void INotifySold.Sold(Actor self) { }
		void INotifySold.Selling(Actor self)
		{
			buildComplete = false;
		}

		void INotifyTransform.BeforeTransform(Actor self)
		{
			buildComplete = false;
		}

		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor self) { }

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}

		void ITick.Tick(Actor self)
		{
			if (randomDelay >= 0)
				randomDelay--;

			if (randomDelay == 0)
				LoopAnimation(self);
		}

		void LoopAnimation(Actor self)
		{
			overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence),
				() => randomDelay = self.World.SharedRandom.Next(info.MinRandomWaitTicks, info.MaxRandomWaitTicks));
		}
	}
}
