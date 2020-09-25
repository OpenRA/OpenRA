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
using OpenRA.Primitives;
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
		public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }

		public abstract bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor);
		public abstract bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor);

		public bool CanTarget(Actor self, in Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
		{
			var type = target.Type;
			if (type != TargetType.Actor && type != TargetType.FrozenActor)
				return false;

			cursor = this.cursor;
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

			if (ForceAttack != null && modifiers.HasModifier(TargetModifiers.ForceAttack) != ForceAttack)
				return false;

			var owner = type == TargetType.FrozenActor ? target.FrozenActor.Owner : target.Actor.Owner;
			var playerRelationship = self.Owner.RelationshipWith(owner);
			if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && playerRelationship == PlayerRelationship.Ally && !targetAllyUnits)
				return false;

			if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && playerRelationship == PlayerRelationship.Enemy && !targetEnemyUnits)
				return false;

			return type == TargetType.FrozenActor ?
				CanTargetFrozenActor(self, target.FrozenActor, modifiers, ref cursor) :
				CanTargetActor(self, target.Actor, modifiers, ref cursor);
		}

		public virtual bool IsQueued { get; protected set; }
	}

	public class TargetTypeOrderTargeter : UnitOrderTargeter
	{
		readonly BitSet<TargetableType> targetTypes;

		public TargetTypeOrderTargeter(BitSet<TargetableType> targetTypes, string order, int priority, string cursor, bool targetEnemyUnits, bool targetAllyUnits)
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
