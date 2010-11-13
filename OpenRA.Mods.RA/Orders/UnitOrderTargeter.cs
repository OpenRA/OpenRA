#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
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

		public virtual bool CanTargetUnit(Actor self, Actor target, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
		{
			if( self == null ) throw new ArgumentNullException( "self" );
			if( target == null ) throw new ArgumentNullException( "target" );

			cursor = this.cursor;
			IsQueued = forceQueued;

			var playerRelationship = self.Owner.Stances[ target.Owner ];

			if( !forceAttack && playerRelationship == Stance.Ally && !targetAllyUnits ) return false;
			if( !forceAttack && playerRelationship == Stance.Enemy && !targetEnemyUnits ) return false;

			return true;
		}

		public virtual bool CanTargetLocation(Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
		{
			return false;
		}
		public virtual bool IsQueued { get; protected set; }
	}

    public class UnitTraitOrderTargeter<T> : UnitOrderTargeter
	{
		public UnitTraitOrderTargeter( string order, int priority, string cursor, bool targetEnemyUnits, bool targetAllyUnits )
			: base( order, priority, cursor, targetEnemyUnits, targetAllyUnits )
		{
		}

		public override bool CanTargetUnit(Actor self, Actor target, bool forceAttack, bool forceMove, bool forceQueued, ref string cursor)
		{
			if( !base.CanTargetUnit( self, target, forceAttack, forceMove, forceQueued, ref cursor ) ) return false;
			if( !target.HasTrait<T>() ) return false;

			IsQueued = forceQueued;

			return true;
		}
	}
}
