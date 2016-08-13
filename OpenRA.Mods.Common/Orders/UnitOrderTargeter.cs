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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public abstract class UnitOrderTargeter : IOrderTargeter
	{
		readonly string cursor;
		readonly bool targetEnemyUnits, targetAllyUnits;

		public UnitOrderTargeter(string order, int priority, string cursor, bool targetEnemyUnits, bool targetAllyUnits)
		{
			OrderID = order;
			OrderPriority = priority;
			this.cursor = cursor;
			this.targetEnemyUnits = targetEnemyUnits;
			this.targetAllyUnits = targetAllyUnits;
		}

		public string OrderID { get; private set; }
		public int OrderPriority { get; private set; }
		public bool? ForceAttack = null;
		public bool TargetOverridesSelection(TargetModifiers modifiers) { return true; }

		public abstract bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor);
		public abstract bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor);

		public bool CanTarget(Actor self, Target target, ref IEnumerable<UIOrder> uiOrders, ref TargetModifiers modifiers)
		{
			if (target.Type != TargetType.Actor && target.Type != TargetType.FrozenActor)
				return false;

			if (ForceAttack != null && modifiers.HasModifier(TargetModifiers.ForceAttack) != ForceAttack)
				return false;

			var owner = target.Type == TargetType.FrozenActor ? target.FrozenActor.Owner : target.Actor.Owner;
			var playerRelationship = self.Owner.Stances[owner];

			if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && playerRelationship == Stance.Ally && !targetAllyUnits)
				return false;

			if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && playerRelationship == Stance.Enemy && !targetEnemyUnits)
				return false;

			return true;
		}

		public bool SetupTarget(Actor self, Target target, List<Actor> othersAtTarget, ref IEnumerable<UIOrder> uiOrders, ref TargetModifiers modifiers, ref string cursor)
		{
			cursor = this.cursor;
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
			return target.Type == TargetType.FrozenActor ?
				CanTargetFrozenActor(self, target.FrozenActor, modifiers, ref cursor) :
				CanTargetActor(self, target.Actor, modifiers, ref cursor);
		}

		public void OrderIssued(Actor self) { }

		public virtual bool IsQueued { get; protected set; }
	}

	public class TargetTypeOrderTargeter : UnitOrderTargeter
	{
		readonly HashSet<string> targetTypes;

		public TargetTypeOrderTargeter(HashSet<string> targetTypes, string order, int priority, string cursor, bool targetEnemyUnits, bool targetAllyUnits)
			: base(order, priority, cursor, targetEnemyUnits, targetAllyUnits)
		{
			this.targetTypes = targetTypes;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			return targetTypes.Overlaps(target.GetEnabledTargetTypes());
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return target.TargetTypes.Overlaps(targetTypes);
		}
	}
}
