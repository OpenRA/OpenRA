using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class Helicopter : ITick, IOrder
	{
		public int altitude;
		public int2 targetLocation;

		const int CruiseAltitude = 20;

		public Helicopter(Actor self)
		{
			targetLocation = self.Location;
		}

		public Order IssueOrder(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if (lmb) return null;

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
				var dist = Game.CellSize * (targetLocation + new float2(.5f,.5f)) - self.CenterLocation;
				var desiredFacing = Util.GetFacing(dist, unit.Facing);
				Util.TickFacing(ref unit.Facing, desiredFacing,
					self.unitInfo.ROT);

				// .6f going the wrong way; .8f going sideways, 1f going forward.
				var rawSpeed = .2f * (self.unitInfo as VehicleInfo).Speed;
				var angle = (unit.Facing - desiredFacing) / 128f * Math.PI;
				var scale = .4f + .6f * (float)Math.Cos(angle);

				if (altitude > CruiseAltitude / 2)		// do some movement.
				{
					self.CenterLocation += (rawSpeed * scale / dist.Length) * dist;
					self.CenterLocation +=  (1f - scale) * rawSpeed 
						* float2.FromAngle((float)angle);
					self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();
				}

				if (altitude < CruiseAltitude)
				{
					++altitude;
					return;
				}
			}
			else if (altitude > 0 && 
				Game.IsCellBuildable( self.Location, UnitMovementType.Foot ))
			{
				--altitude;
			}

			/* todo: bob slightly when hovering */
		}
	}
}
