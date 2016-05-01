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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders an overlay when the actor is taking heavy damage.")]
	public class WithDamageOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly string Image = "smoke_m";

		[SequenceReference("Image")] public readonly string IdleSequence = "idle";
		[SequenceReference("Image")] public readonly string LoopSequence = "loop";
		[SequenceReference("Image")] public readonly string EndSequence = "end";

		[Desc("Damage types that this should be used for (defined on the warheads).",
			"Leave empty to disable all filtering.")]
		public readonly HashSet<string> DamageTypes = new HashSet<string>();

		[Desc("Trigger when Undamaged, Light, Medium, Heavy, Critical or Dead.")]
		public readonly DamageState MinimumDamageState = DamageState.Heavy;
		public readonly DamageState MaximumDamageState = DamageState.Dead;

		public object Create(ActorInitializer init) { return new WithDamageOverlay(init.Self, this); }
	}

	public class WithDamageOverlay : INotifyDamage
	{
		readonly WithDamageOverlayInfo info;
		readonly Animation anim;

		bool isSmoking;

		public WithDamageOverlay(Actor self, WithDamageOverlayInfo info)
		{
			this.info = info;

			var rs = self.Trait<RenderSprites>();

			anim = new Animation(self.World, info.Image);
			rs.Add(new AnimationWithOffset(anim, null, () => !isSmoking));
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			var warhead = e.Warhead as DamageWarhead;
			if (info.DamageTypes.Count > 0 && (warhead != null && !warhead.DamageTypes.Overlaps(info.DamageTypes)))
				return;

			if (isSmoking) return;
			if (e.Damage < 0) return;	/* getting healed */
			if (e.DamageState < info.MinimumDamageState) return;
			if (e.DamageState > info.MaximumDamageState) return;

			isSmoking = true;
			anim.PlayThen(info.IdleSequence,
				() => anim.PlayThen(info.LoopSequence,
					() => anim.PlayThen(info.EndSequence,
						() => isSmoking = false)));
		}
	}
}
