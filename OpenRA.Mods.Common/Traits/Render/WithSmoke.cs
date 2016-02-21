#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Renders an overlay when the actor is taking heavy damage.")]
	public class WithSmokeInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly string Sequence = "smoke_m";

		[SequenceReference("Sequence")] public readonly string IdleSequence = "idle";
		[SequenceReference("Sequence")] public readonly string LoopSequence = "loop";
		[SequenceReference("Sequence")] public readonly string EndSequence = "end";

		public object Create(ActorInitializer init) { return new WithSmoke(init.Self, this); }
	}

	public class WithSmoke : INotifyDamage
	{
		readonly WithSmokeInfo info;
		readonly Animation anim;

		bool isSmoking;

		public WithSmoke(Actor self, WithSmokeInfo info)
		{
			this.info = info;

			var rs = self.Trait<RenderSprites>();

			anim = new Animation(self.World, info.Sequence);
			rs.Add(new AnimationWithOffset(anim, null, () => !isSmoking));
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (isSmoking) return;
			if (e.Damage < 0) return;	/* getting healed */
			if (e.DamageState < DamageState.Heavy) return;

			isSmoking = true;
			anim.PlayThen(info.IdleSequence,
				() => anim.PlayThen(info.LoopSequence,
					() => anim.PlayBackwardsThen(info.EndSequence,
						() => isSmoking = false)));
		}
	}
}
