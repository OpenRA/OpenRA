﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
{
	public class UnitOrderTargeter : IOrderTargeter
	{
		readonly string cursor;
		readonly bool targetEnemyUnits, targetAllyUnits;

		public UnitOrderTargeter( string order, int priority, string cursor, bool targetEnemyUnits, bool targetAllyUnits )
		{
			this.OrderID = order;
			this.OrderPriority = priority;
			this.cursor = cursor;
			this.targetEnemyUnits = targetEnemyUnits;
			this.targetAllyUnits = targetAllyUnits;
		}

		public string OrderID { get; private set; }
		public int OrderPriority { get; private set; }
		public bool? ForceAttack = null;

		public virtual bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if( self == null ) throw new ArgumentNullException( "self" );
			if( target == null ) throw new ArgumentNullException( "target" );

			cursor = this.cursor;
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

			if (ForceAttack != null && modifiers.HasModifier(TargetModifiers.ForceAttack) != ForceAttack) return false;

			var playerRelationship = self.Owner.Stances[target.Owner];

			if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && playerRelationship == Stance.Ally && !targetAllyUnits) return false;
			if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && playerRelationship == Stance.Enemy && !targetEnemyUnits) return false;

			return true;
		}

		public virtual bool CanTargetLocation(Actor self, CPos location, List<Actor> actorsAtLocation, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}

		public virtual bool IsQueued { get; protected set; }
	}

	public class TargetTypeOrderTargeter : UnitOrderTargeter
	{
		string targetType;

		public TargetTypeOrderTargeter(string targetType, string order, int priority, string cursor, bool targetEnemyUnits, bool targetAllyUnits)
			: base(order, priority, cursor, targetEnemyUnits, targetAllyUnits)
		{
			this.targetType = targetType;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (!base.CanTargetActor(self, target, modifiers, ref cursor))
				return false;

			if (!target.TraitsImplementing<ITargetable>().Any(t => t.TargetTypes.Contains(targetType)))
			    return false;

			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

			return true;
		}
	}
}
