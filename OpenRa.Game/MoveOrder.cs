using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	abstract class Order
	{
		public abstract void Apply();
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

		public override void Apply()
		{
			if (Game.LocalPlayer == Unit.Owner)
				Game.PlaySound(Game.SovietVoices.First.GetNext() + GetVoiceSuffix(), false);

			var mobile = Unit.traits.Get<Mobile>();
			mobile.Cancel(Unit);
			mobile.QueueActivity( new Mobile.MoveTo( Destination ) );

			var attackBase = Unit.traits.WithInterface<AttackBase>().FirstOrDefault();
			if (attackBase != null)
				attackBase.target = null;	/* move cancels attack order */
		}
	}
}
