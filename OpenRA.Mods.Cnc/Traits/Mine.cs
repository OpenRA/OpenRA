#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class MineInfo : TraitInfo
	{
		public readonly BitSet<CrushClass> CrushClasses = default(BitSet<CrushClass>);
		public readonly bool AvoidFriendly = true;
		public readonly bool BlockFriendly = true;
		public readonly BitSet<CrushClass> DetonateClasses = default(BitSet<CrushClass>);

		public override object Create(ActorInitializer init) { return new Mine(this); }
	}

	class Mine : ICrushable, INotifyCrushed
	{
		readonly MineInfo info;

		public Mine(MineInfo info)
		{
			this.info = info;
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses) { }

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			if (!info.CrushClasses.Overlaps(crushClasses))
				return;

			if (crusher.Info.HasTraitInfo<MineImmuneInfo>() || (self.Owner.RelationshipWith(crusher.Owner) == PlayerRelationship.Ally && info.AvoidFriendly))
				return;

			var mobile = crusher.TraitOrDefault<Mobile>();
			if (mobile != null && !info.DetonateClasses.Overlaps(mobile.Info.LocomotorInfo.Crushes))
				return;

			self.Kill(crusher, mobile != null ? mobile.Info.LocomotorInfo.CrushDamageTypes : default(BitSet<DamageType>));
		}

		bool ICrushable.CrushableBy(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			if (info.BlockFriendly && !crusher.Info.HasTraitInfo<MineImmuneInfo>() && self.Owner.RelationshipWith(crusher.Owner) == PlayerRelationship.Ally)
				return false;

			return info.CrushClasses.Overlaps(crushClasses);
		}

		LongBitSet<PlayerBitMask> ICrushable.CrushableBy(Actor self, BitSet<CrushClass> crushClasses)
		{
			if (!info.CrushClasses.Overlaps(crushClasses))
				return self.World.NoPlayersMask;

			// Friendly units should move around!
			return info.BlockFriendly ? ~self.Owner.AlliedPlayersMask : self.World.AllPlayersMask;
		}
	}

	[Desc("Tag trait for stuff that should not trigger mines.")]
	class MineImmuneInfo : TraitInfo<MineImmune> { }
	class MineImmune { }
}
