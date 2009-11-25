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

		public IActivity Tick( Actor self )
		{
			var unit = self.traits.Get<Unit>();

			if( desiredFacing == unit.Facing )
				return NextActivity;

			Util.TickFacing( ref unit.Facing, desiredFacing, self.unitInfo.ROT );
			return null;
		}

		public void Cancel( Actor self )
		{
			var unit = self.traits.Get<Unit>();

			desiredFacing = unit.Facing;
			NextActivity = null;
		}
	}
}
