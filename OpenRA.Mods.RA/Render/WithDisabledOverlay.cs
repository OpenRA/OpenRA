#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Use together with DisabledByWarhead to render an overlay.", "Ex: EMP Sparkle Overlay")]
	public class WithDisabledOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		[Desc("The name of the sequence to play","Needs 2 sub-sequences. A \"loop\" and an \"end\" sub-sequence.")]
		public readonly string Sequence = "smoke_m";

		[Desc("Position relative to body")]
		public readonly int zOffset = 10;

		public object Create(ActorInitializer init) { return new WithDisabledOverlay(init.self, this); }
	}

	public class WithDisabledOverlay : IRenderModifier, INotifyDisabledByWarheadState
	{
		bool isShowingDisabled;
		Animation anim;

		public WithDisabledOverlay(Actor self, WithDisabledOverlayInfo info)
		{
			var rs = self.Trait<RenderSprites>();

			anim = new Animation(self.World, info.Sequence);
			rs.Add("sparks", new AnimationWithOffset(anim, null, () => !isShowingDisabled, info.zOffset));
		}

		public void DisabledByWarheadStateChanged(Actor self, bool isNowDisabled)
		{
			if (!isNowDisabled)
			{
				if (isShowingDisabled)
					anim.PlayThen("end", () => isShowingDisabled = false);
				return;
			}

			if (isShowingDisabled) return;

			isShowingDisabled = true;
			anim.PlayRepeating("loop");
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			var disablerTrait = self.TraitOrDefault<DisabledByWarhead>();
			var disabled = disablerTrait.Disabled;

			foreach (var a in r)
			{
				yield return a;
				if (disabled && !a.IsDecoration)
				{
					yield return a.WithPalette(wr.Palette("disabled"))
						.WithZOffset(a.ZOffset + 1)
						.AsDecoration();
				}
			}
		}
	}
}