using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class Mobile : ITick, IOrder
	{
		public Actor self;

		public int2 fromCell, destination;
		public int2 toCell { get { return self.Location; } }
		public int moveFraction, moveFractionTotal;
		public int facing;
		public int? desiredFacing;
		public int Voice = Game.CosmeticRandom.Next(2);

		public Mobile(Actor self)
		{
			this.self = self;
			fromCell = destination = self.Location;
		}

		void UpdateCenterLocation()
		{
			float fraction = (moveFraction > 0) ? (float)moveFraction / moveFractionTotal : 0f;
			self.CenterLocation = new float2(12, 12) + Game.CellSize * float2.Lerp(fromCell, toCell, fraction);
		}

		public void Tick(Actor self)
		{
			Move(self);
			UpdateCenterLocation();
		}

		void Move(Actor self)
		{
			if( fromCell != toCell )
				desiredFacing = Util.GetFacing( toCell - fromCell, facing );

			if( desiredFacing != null && desiredFacing != facing )
			{
				Util.TickFacing( ref facing, desiredFacing.Value, self.unitInfo.ROT );
				return;
			}
			desiredFacing = null;

			if( fromCell != toCell )
				moveFraction += ((UnitInfo.MobileInfo)self.unitInfo).Speed;

			if (moveFraction < moveFractionTotal)
				return;

			moveFraction = 0;
			moveFractionTotal = 0;
			fromCell = toCell;

			if (destination == toCell)
				return;

			List<int2> res = Game.pathFinder.FindUnitPath(toCell, PathFinder.DefaultEstimator(destination));
			if (res.Count != 0)
			{
				self.Location = res[res.Count - 1];

				int2 dir = toCell - fromCell;
				moveFractionTotal = (dir.X != 0 && dir.Y != 0) ? 70 : 50;
			}
			else
				destination = toCell;
		}

		public Order Order(Actor self, int2 xy)
		{
			if (xy != toCell)
				return new MoveOrder(self, xy);

			return null;
		}
	}
}
