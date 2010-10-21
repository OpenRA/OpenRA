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
using System.Linq;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class Enter : CancelableActivity
	{
		readonly Actor target;
		public Enter( Actor target ) { this.target = target; }

		public override IActivity Tick( Actor self )
		{
			if( IsCanceled || target.Destroyed || !target.IsInWorld )
				return NextActivity;

			var mobile = self.Trait<Mobile>();
			var nearest = target.Trait<IOccupySpace>().NearestCellTo( mobile.toCell );
			if( ( nearest - mobile.toCell ).LengthSquared > 2 )
				return Util.SequenceActivities( new MoveAdjacentTo( target ), this );

			return Util.SequenceActivities( mobile.MoveTo( nearest, target ), NextActivity );
		}
	}
}
