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

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class AttackOmniInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackOmni(init.Self, this); }
	}

	public class AttackOmni : AttackBase
	{
		public AttackOmni(Actor self, AttackOmniInfo info)
			: base(self, info) { }

		public override Activity GetAttackActivity(Actor self, AttackSource source, in Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor = null)
		{
			return new SetTarget(this, newTarget, allowMove, forceAttack, targetLineColor);
		}

		// Some 3rd-party mods rely on this being public
		public class SetTarget : Activity, IActivityNotifyStanceChanged
		{
			readonly AttackOmni attack;
			readonly bool allowMove;
			readonly bool forceAttack;
			readonly Color? targetLineColor;
			Target target;

			public SetTarget(AttackOmni attack, in Target target, bool allowMove, bool forceAttack, Color? targetLineColor = null)
			{
				this.target = target;
				this.targetLineColor = targetLineColor;
				this.attack = attack;
				this.allowMove = allowMove;
				this.forceAttack = forceAttack;
			}

			public override bool Tick(Actor self)
			{
				// This activity can't move to reacquire hidden targets, so use the
				// Recalculate overload that invalidates hidden targets.
				target = target.RecalculateInvalidatingHiddenTargets(self.Owner);
				if (IsCanceling || !target.IsValidFor(self) || !attack.IsReachableTarget(target, allowMove))
					return true;

				attack.DoAttack(self, target);
				return false;
			}

			void IActivityNotifyStanceChanged.StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance)
			{
				// Cancel non-forced targets when switching to a more restrictive stance if they are no longer valid for auto-targeting
				if (newStance > oldStance || forceAttack)
					return;

				if (target.Type == TargetType.Actor)
				{
					var a = target.Actor;
					if (!autoTarget.HasValidTargetPriority(self, a.Owner, a.GetEnabledTargetTypes()))
						target = Target.Invalid;
				}
				else if (target.Type == TargetType.FrozenActor)
				{
					var fa = target.FrozenActor;
					if (!autoTarget.HasValidTargetPriority(self, fa.Owner, fa.TargetTypes))
						target = Target.Invalid;
				}
			}

			public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
			{
				if (targetLineColor != null)
					yield return new TargetLineNode(target, targetLineColor.Value);
			}
		}
	}
}
