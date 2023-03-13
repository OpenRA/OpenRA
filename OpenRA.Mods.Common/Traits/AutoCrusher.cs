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

	sealed class AutoCrusher : PausableConditionalTrait<AutoCrusherInfo>, INotifyIdle
	{
		int nextScanTime;
		readonly IMoveInfo moveInfo;
		readonly bool isAircraft;
		readonly IMove move;

		public AutoCrusher(Actor self, AutoCrusherInfo info)
			: base(info)
		{
			move = self.Trait<IMove>();
			moveInfo = self.Info.TraitInfo<IMoveInfo>();
			nextScanTime = self.World.SharedRandom.Next(Info.MinimumScanTimeInterval, Info.MaximumScanTimeInterval);
			isAircraft = move is Aircraft;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (nextScanTime-- > 0)
				return;

			// TODO: Add a proper Cloak and Disguise detection here.
			var crushableActor = self.World.FindActorsInCircle(self.CenterPosition, Info.ScanRadius)
				.Where(a => a != self && !a.IsDead && a.IsInWorld &&
					self.Location != a.Location && a.IsAtGroundLevel() &&
					Info.TargetRelationships.HasRelationship(self.Owner.RelationshipWith(a.Owner)) &&
					a.TraitsImplementing<ICrushable>().Any(c => c.CrushableBy(a, self, Info.CrushClasses)))
				.ClosestTo(self); // TODO: Make it use shortest pathfinding distance instead

			if (crushableActor == null)
				return;

			if (isAircraft)
				self.QueueActivity(new Land(self, Target.FromActor(crushableActor), targetLineColor: moveInfo.GetTargetLineColor()));
			else
				self.QueueActivity(move.MoveTo(crushableActor.Location, targetLineColor: moveInfo.GetTargetLineColor()));

			nextScanTime = self.World.SharedRandom.Next(Info.MinimumScanTimeInterval, Info.MaximumScanTimeInterval);
		}
	}
}
