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
	class RenderUnitSpinnerInfo : RenderUnitInfo
	{
		public readonly int[] Offset = { 0, 0 };
		public override object Create(Actor self) { return new RenderUnitSpinner(self); }
	}

	class RenderUnitSpinner : RenderUnit
	{
		public RenderUnitSpinner( Actor self )
			: base(self)
		{
			var unit = self.traits.Get<Unit>();
			var info = self.Info.Traits.Get<RenderUnitSpinnerInfo>();

			var spinnerAnim = new Animation( GetImage(self) );
			spinnerAnim.PlayRepeating( "spinner" );
			anims.Add( "spinner", new AnimationWithOffset(
				spinnerAnim,
				() => Util.GetTurretPosition( self, unit, info.Offset, 0 ),
				null ) { ZOffset = 1 } );
		}
	}
}
