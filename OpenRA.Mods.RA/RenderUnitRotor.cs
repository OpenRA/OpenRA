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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RenderUnitRotorInfo : RenderUnitInfo
	{
		public readonly int[] PrimaryOffset = { 0, 0 };
		public readonly int[] SecondaryOffset = null;

		public override object Create(Actor self) { return new RenderUnitRotor(self); }
	}

	class RenderUnitRotor : RenderUnit
	{
		public Animation rotorAnim, secondRotorAnim;

		public RenderUnitRotor( Actor self )
			: base(self)
		{
			var unit = self.traits.Get<Unit>();
			var info = self.Info.Traits.Get<RenderUnitRotorInfo>();

			rotorAnim = new Animation(GetImage(self));
			rotorAnim.PlayRepeating("rotor");
			anims.Add( "rotor_1", new AnimationWithOffset(
				rotorAnim,
				() => Util.GetTurretPosition( self, unit, info.PrimaryOffset, 0 ),
				null ) { ZOffset = 1 } );

			if (info.SecondaryOffset == null) return;

			secondRotorAnim = new Animation(GetImage(self));
			secondRotorAnim.PlayRepeating( "rotor2" );
			anims.Add( "rotor_2", new AnimationWithOffset(
				secondRotorAnim,
				() => Util.GetTurretPosition(self, unit, info.SecondaryOffset, 0),
				null) { ZOffset = 1 });
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			var unit = self.traits.Get<Unit>();
			
			var isFlying = unit.Altitude > 0;

			if (isFlying ^ (rotorAnim.CurrentSequence.Name != "rotor")) 
				return;

			rotorAnim.ReplaceAnim(isFlying ? "rotor" : "slow-rotor");
			if (secondRotorAnim != null)
				secondRotorAnim.ReplaceAnim(isFlying ? "rotor2" : "slow-rotor2");
		}
	}
}
