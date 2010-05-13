#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Linq;
using System;

namespace OpenRA.Traits
{
	class RenderBuildingWallInfo : RenderBuildingInfo
	{
		public readonly int DamageStates = 2;
		public override object Create(Actor self) { return new RenderBuildingWall(self); }
	}

	class RenderBuildingWall : RenderBuilding
	{
		string seqName;
		int adjacentWalls = 0;
		
		public RenderBuildingWall(Actor self)
			: base(self)
		{
			seqName = "idle";
			anim.PlayFetchIndex(seqName, () => adjacentWalls);
		}

		enum ExtendedDamageState { Normal, ThreeQuarter, Half, Quarter, Dead };

		ExtendedDamageState GetExtendedState( Actor self, int damage )
		{
			var effectiveHealth = self.Health + damage;

			if (effectiveHealth <= 0)
				return ExtendedDamageState.Dead;

			if (effectiveHealth < self.GetMaxHP() * self.World.Defaults.ConditionRed)
				return ExtendedDamageState.Quarter;

			if (effectiveHealth < self.GetMaxHP() * self.World.Defaults.ConditionYellow)
				return ExtendedDamageState.Half;

			if (effectiveHealth < self.GetMaxHP() * 0.75)
				return ExtendedDamageState.ThreeQuarter;

			return ExtendedDamageState.Normal;
		}

		public override void Damaged(Actor self, AttackInfo e)
		{
			var oldState = GetExtendedState(self, e.Damage);
			var newState = GetExtendedState(self, 0);

			var numStates = self.Info.Traits.Get<RenderBuildingWallInfo>().DamageStates;

			if (oldState == newState) return;

			switch (newState)
			{
				case ExtendedDamageState.Normal:
					seqName = "idle";
					break;
				case ExtendedDamageState.ThreeQuarter:
					if (numStates >= 4)
						seqName = "minor-damaged-idle";
					break;
				case ExtendedDamageState.Half:
					seqName = "damaged-idle";
					Sound.Play(self.Info.Traits.Get<BuildingInfo>().DamagedSound, self.CenterLocation);
					break;
				case ExtendedDamageState.Quarter:
					if (numStates >= 3)
					{
						seqName = "critical-idle";
						Sound.Play(self.Info.Traits.Get<BuildingInfo>().DamagedSound, self.CenterLocation);
					}
					break;
			}

			anim.PlayFetchIndex(seqName, () => adjacentWalls);
		}

		bool hasTicked = false;
		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (!hasTicked)
			{
				var oneCell = new float2(Game.CellSize, Game.CellSize);
				var adjWalls = self.World.FindUnits(self.CenterLocation - oneCell, self.CenterLocation + oneCell)
					.Where(a => a.Info == self.Info && a != self);

				foreach (var w in adjWalls)
				{
					w.traits.Get<RenderBuildingWall>().AddAdjacentWall(w, self);
					AddAdjacentWall(self, w);
				}
				hasTicked = true;
			}
		}

		void AddAdjacentWall(Actor self, Actor other)
		{
			if (other.Location == self.Location + new int2(0, -1)) adjacentWalls |= 1;
			if (other.Location == self.Location + new int2(+1, 0)) adjacentWalls |= 2;
			if (other.Location == self.Location + new int2(0, +1)) adjacentWalls |= 4;
			if (other.Location == self.Location + new int2(-1, 0)) adjacentWalls |= 8;
		}
	}
}
