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

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackOmniInfo : AttackBaseInfo
	{
		public override object Create(Actor self) { return new AttackOmni(self); }
	}

	class AttackOmni : AttackBase, INotifyBuildComplete
	{
		bool buildComplete = false;
		public void BuildingComplete(Actor self) { buildComplete = true; }

		public AttackOmni(Actor self) : base(self) { }

		protected override bool CanAttack( Actor self )
		{
			var isBuilding = ( self.traits.Contains<Building>() && !buildComplete );
			return base.CanAttack( self ) && !isBuilding;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			DoAttack(self);
		}

		protected override void QueueAttack(Actor self, Order order)
		{
			target = order.TargetActor;
		}
	}
}
