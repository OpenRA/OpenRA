using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Turn : IActivity
	{
		public IActivity NextActivity { get; set; }

		public int desiredFacing;

		public Turn( int desiredFacing )
		{
			this.desiredFacing = desiredFacing;
		}

		public IActivity Tick( Actor self, Mobile mobile )
		{
			if( desiredFacing == mobile.facing )
				return NextActivity;

			Util.TickFacing( ref mobile.facing, desiredFacing, self.unitInfo.ROT );
			return null;
		}

		public void Cancel( Actor self, Mobile mobile )
		{
			desiredFacing = mobile.facing;
			NextActivity = null;
		}
	}
}
