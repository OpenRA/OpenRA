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

		public Mobile(Actor self)
		{
			this.self = self;
			fromCell = destination = self.Location;
		}

		public bool Turn(int desiredFacing)
		{
			if (facing == desiredFacing)
				return false;

			int df = (desiredFacing - facing + 32) % 32;
			facing = (facing + (df > 16 ? 31 : 1)) % 32;
			return true;
		}

		static float2[] fvecs = Util.MakeArray<float2>(32,
			i => -float2.FromAngle(i / 16.0f * (float)Math.PI) * new float2(1f, 1.3f));

		int GetFacing(float2 d)
		{
			if (float2.WithinEpsilon(d, float2.Zero, 0.001f))
				return facing;

			int highest = -1;
			float highestDot = -1.0f;

			for (int i = 0; i < fvecs.Length; i++)
			{
				float dot = float2.Dot(fvecs[i], d);
				if (dot > highestDot)
				{
					highestDot = dot;
					highest = i;
				}
			}

			return highest;
		}

		void UpdateCenterLocation()
		{
			float fraction = (moveFraction > 0) ? (float)moveFraction / moveFractionTotal : 0f;
			self.CenterLocation = new float2(12, 12) + Game.CellSize * float2.Lerp(fromCell, toCell, fraction);
		}

		public void Tick(Actor self, Game game, int dt)
		{
			Move(self, game, dt);
			UpdateCenterLocation();
		}

		void Move(Actor self, Game game, int dt)
		{
			if (fromCell != toCell)
			{
				if (Turn(GetFacing(toCell - fromCell)))
					return;

				moveFraction += dt * ((UnitInfo.MobileInfo)self.unitInfo).Speed;
			}
			if (moveFraction < moveFractionTotal)
				return;

			moveFraction = 0;
			moveFractionTotal = 0;
			fromCell = toCell;

			if (destination == toCell)
				return;

			List<int2> res = game.pathFinder.FindUnitPath(toCell, PathFinder.DefaultEstimator(destination));
			if (res.Count != 0)
			{
				self.Location = res[res.Count - 1];

				int2 dir = toCell - fromCell;
				moveFractionTotal = (dir.X != 0 && dir.Y != 0) ? 2500 : 2000;
			}
			else
				destination = toCell;
		}

		public Order Order(Actor self, Game game, int2 xy)
		{
			if (xy != toCell)
			{
				return new MoveOrder(self, xy);
			}
			return null;
		}
	}
}
