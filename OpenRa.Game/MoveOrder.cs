using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	abstract class Order
	{
		public abstract void Apply( bool leftMButton );
	}

	class MoveOrder : Order
	{
		public readonly Actor Unit;
		public readonly int2 Destination;

		public MoveOrder( Actor unit, int2 destination )
		{
			this.Unit = unit;
			this.Destination = destination;
		}

		string GetVoiceSuffix()
		{
			var suffixes = new[] { ".r01", ".r03" };
			return suffixes[Unit.traits.Get<Traits.Mobile>().Voice];
		}

		public override void Apply( bool leftMouseButton )
		{
			if (leftMouseButton) return;
			
			if (Game.LocalPlayer == Unit.Owner)
				Game.PlaySound("ackno.r00", false);
			var mobile = Unit.traits.Get<Traits.Mobile>();
			mobile.QueueAction( new Traits.Mobile.MoveTo( Destination ) );
		}
	}
}
