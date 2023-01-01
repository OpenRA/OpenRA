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

using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor is crushable.")]
	class CrushableInfo : ConditionalTraitInfo
	{
		[Desc("Sound to play when being crushed.")]
		public readonly string CrushSound = null;
		[Desc("Which crush classes does this actor belong to.")]
		public readonly BitSet<CrushClass> CrushClasses = new BitSet<CrushClass>("infantry");
		[Desc("Probability of mobile actors noticing and evading a crush attempt.")]
		public readonly int WarnProbability = 75;
		[Desc("Will friendly units just crush me instead of pathing around.")]
		public readonly bool CrushedByFriendlies = false;

		public override object Create(ActorInitializer init) { return new Crushable(init.Self, this); }
	}

	class Crushable : ConditionalTrait<CrushableInfo>, ICrushable, INotifyCrushed
	{
		readonly Actor self;

		public Crushable(Actor self, CrushableInfo info)
			: base(info)
		{
			this.self = self;
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			if (!CrushableInner(crushClasses, crusher.Owner))
				return;

			var mobile = self.TraitOrDefault<Mobile>();
			if (mobile != null && self.World.SharedRandom.Next(100) <= Info.WarnProbability)
				mobile.Nudge(crusher);
		}

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			if (!CrushableInner(crushClasses, crusher.Owner))
				return;

			Game.Sound.Play(SoundType.World, Info.CrushSound, crusher.CenterPosition);

			var crusherMobile = crusher.TraitOrDefault<Mobile>();
			self.Kill(crusher, crusherMobile != null ? crusherMobile.Info.LocomotorInfo.CrushDamageTypes : default);
		}

		bool ICrushable.CrushableBy(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			return CrushableInner(crushClasses, crusher.Owner);
		}

		LongBitSet<PlayerBitMask> ICrushable.CrushableBy(Actor self, BitSet<CrushClass> crushClasses)
		{
			if (IsTraitDisabled || !Info.CrushClasses.Overlaps(crushClasses))
				return self.World.NoPlayersMask;

			return Info.CrushedByFriendlies ? self.World.AllPlayersMask : self.World.AllPlayersMask.Except(self.Owner.AlliedPlayersMask);
		}

		bool CrushableInner(BitSet<CrushClass> crushClasses, Player crushOwner)
		{
			if (IsTraitDisabled)
				return false;

			if (!Info.CrushedByFriendlies && crushOwner.IsAlliedWith(self.Owner))
				return false;

			return Info.CrushClasses.Overlaps(crushClasses);
		}

		protected override void TraitEnabled(Actor self)
		{
			self.World.ActorMap.UpdateOccupiedCells(self.OccupiesSpace);
		}

		protected override void TraitDisabled(Actor self)
		{
			self.World.ActorMap.UpdateOccupiedCells(self.OccupiesSpace);
		}
	}
}
