using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Turn : Activity
	{
		public Activity NextActivity { get; set; }

		public int desiredFacing;

		public Turn( int desiredFacing )
		{
			this.desiredFacing = desiredFacing;
		}

		public void Tick( Actor self, Mobile mobile )
		{
			if( desiredFacing == mobile.facing )
			{
				mobile.InternalSetActivity( NextActivity );
				if( NextActivity != null )
					NextActivity.Tick( self, mobile );
				return;
			}
			Util.TickFacing( ref mobile.facing, desiredFacing, self.unitInfo.ROT );
		}

		public void Cancel( Actor self, Mobile mobile )
		{
			desiredFacing = mobile.facing;
			NextActivity = null;
		}
	}
}
