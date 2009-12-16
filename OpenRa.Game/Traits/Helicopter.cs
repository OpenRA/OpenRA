using System;
using System.Linq;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class Helicopter : ITick, IOrder
	{
		public int2 targetLocation;

		const int CruiseAltitude = 20;

		public Helicopter(Actor self)
		{
			targetLocation = self.Location;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor == null)
				return Order.Move(self, xy);

			return null;
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if( order.OrderString == "Move" )
			{
				targetLocation = order.TargetLocation;

				var attackBase = self.traits.WithInterface<AttackBase>().FirstOrDefault();
				if( attackBase != null )
					attackBase.target = null;	/* move cancels attack order */
			}
		}

		public void Tick(Actor self)
		{
			var unit = self.traits.Get<Unit>();

			if (self.Location != targetLocation)
			{
				var dist = Util.CenterOfCell(targetLocation) - self.CenterLocation;
				var desiredFacing = Util.GetFacing(dist, unit.Facing);
				Util.TickFacing(ref unit.Facing, desiredFacing,
					self.Info.ROT);

				// .6f going the wrong way; .8f going sideways, 1f going forward.
				var rawSpeed = .2f * (self.Info as VehicleInfo).Speed;
				var angle = (unit.Facing - desiredFacing) / 128f * Math.PI;
				var scale = .4f + .6f * (float)Math.Cos(angle);

				if (unit.Altitude > CruiseAltitude / 2)		// do some movement.
				{
					self.CenterLocation += (rawSpeed * scale / dist.Length) * dist;
					self.CenterLocation +=  (1f - scale) * rawSpeed 
						* float2.FromAngle((float)angle);
					self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();
				}

				if (unit.Altitude < CruiseAltitude)
				{
					++unit.Altitude;
					return;
				}
			}
			else if (unit.Altitude > 0 && 
				Game.IsCellBuildable( self.Location, UnitMovementType.Foot ))
			{
				--unit.Altitude;
			}

			/* todo: bob slightly when hovering */
		}
	}
}
