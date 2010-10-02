using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
{
	class EnterBuildingOrderTargeter<T> : UnitTraitOrderTargeter<T>
	{
		readonly Func<Actor, bool> canTarget;
		readonly Func<Actor, bool> useEnterCursor;

		public EnterBuildingOrderTargeter( string order, int priority, bool targetEnemy, bool targetAlly,
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
