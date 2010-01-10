using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitSpinnerInfo : RenderUnitInfo
	{
		public override object Create(Actor self) { return new RenderUnitSpinner(self); }
	}

	class RenderUnitSpinner : RenderUnit
	{
		public RenderUnitSpinner( Actor self )
			: base(self)
		{
			var unit = self.traits.Get<Unit>();

			var spinnerAnim = new Animation( self.LegacyInfo.Name );
			spinnerAnim.PlayRepeating( "spinner" );
			anims.Add( "spinner", new AnimationWithOffset(
				spinnerAnim,
				() => Util.GetTurretPosition( self, unit, self.LegacyInfo.PrimaryOffset, 0 ),
				null ) );
		}
	}
}
