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

namespace OpenRA.Mods.RA.Orders
{
	class EnterOrderTargeter<T> : UnitTraitOrderTargeter<T>
	{
		readonly Func<Actor, bool> canTarget;
		readonly Func<Actor, bool> useEnterCursor;

		public EnterOrderTargeter( string order, int priority, bool targetEnemy, bool targetAlly,
			Func<Actor, bool> canTarget, Func<Actor, bool> useEnterCursor )
			: base( order, priority, "enter", targetEnemy, targetAlly )
		{
			this.canTarget = canTarget;
			this.useEnterCursor = useEnterCursor;
		}

		public override bool CanTargetUnit( Actor self, Actor target, bool forceAttack, bool forceMove, ref string cursor )
		{
			if( !base.CanTargetUnit( self, target, forceAttack, forceMove, ref cursor ) ) return false;
			if( !canTarget( target ) ) return false;
			cursor = useEnterCursor( target ) ? "enter" : "enter-blocked";
			return true;
		}
	}
}
