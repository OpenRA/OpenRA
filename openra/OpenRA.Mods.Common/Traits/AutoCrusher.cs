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

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	sealed class AutoCrusherInfo : PausableConditionalTraitInfo, Requires<IMoveInfo>
	{
		[Desc("Maximum range to scan for targets.")]
		public readonly WDist ScanRadius = WDist.FromCells(5);

		[Desc("The minimal amount of ticks to wait between scanning for targets.")]
		public readonly int MinimumScanTimeInterval = 10;

		[Desc("The maximal amount of ticks to wait between scanning for targets.")]
		public readonly int MaximumScanTimeInterval = 15;

		[Desc("The crush class(es) that can be automatically crushed.")]
		public readonly BitSet<CrushClass> CrushClasses = default;

		[Desc("Player relationships the owner of the actor needs to get targeted.")]
		public readonly PlayerRelationship TargetRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new AutoCrusher(init.Self, this); }
	}

	sealed class AutoCrusher : ConditionalTrait<AutoCrusherInfo>, INotifyIdle
	{
		int nextScanTime;
		readonly IMoveInfo moveInfo;
		readonly bool isAircraft;
		readonly bool ignoresDisguise;
		readonly IMove move;

		public AutoCrusher(Actor self, AutoCrusherInfo info)
			: base(info)
		{
			move = self.Trait<IMove>();
			moveInfo = self.Info.TraitInfo<IMoveInfo>();
			isAircraft = move is Aircraft;
			ignoresDisguise = self.Info.HasTraitInfo<IgnoresDisguiseInfo>();
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled || nextScanTime-- > 0)
				return;

			var crushableActor = self.World.FindActorsInCircle(self.CenterPosition, Info.ScanRadius)
				.Where(a => IsValidCrushTarget(self, a))
				.ClosestToWithPathFrom(self); // TODO: Make it use shortest pathfinding distance instead

			if (crushableActor == null)
				return;

			if (isAircraft)
				self.QueueActivity(new Land(self, Target.FromActor(crushableActor), targetLineColor: moveInfo.GetTargetLineColor()));
			else
				self.QueueActivity(move.MoveTo(crushableActor.Location, targetLineColor: moveInfo.GetTargetLineColor()));

			nextScanTime = self.World.SharedRandom.Next(Info.MinimumScanTimeInterval, Info.MaximumScanTimeInterval);
		}

		bool IsValidCrushTarget(Actor self, Actor target)
		{
			if (target == self || target.IsDead || !target.IsInWorld || self.Location == target.Location || !target.IsAtGroundLevel())
				return false;

			var targetRelationship = self.Owner.RelationshipWith(target.Owner);
			var effectiveOwner = target.EffectiveOwner?.Owner;
			if (effectiveOwner != null && !ignoresDisguise && targetRelationship != PlayerRelationship.Ally)
			{
				// Check effective relationships if the target is disguised and we cannot see through the disguise. (By ignoring it or by being an ally.)
				if (!Info.TargetRelationships.HasRelationship(self.Owner.RelationshipWith(effectiveOwner)))
					return false;
			}
			else if (!Info.TargetRelationships.HasRelationship(targetRelationship))
				return false;

			if (target.TraitsImplementing<Cloak>().Any(c => !c.IsTraitDisabled && !c.IsVisible(target, self.Owner)))
				return false;

			return target.Crushables.Any(c => c.CrushableBy(target, self, Info.CrushClasses));
		}

		protected override void TraitEnabled(Actor self)
		{
			nextScanTime = self.World.SharedRandom.Next(Info.MinimumScanTimeInterval, Info.MaximumScanTimeInterval);
		}
	}
}
