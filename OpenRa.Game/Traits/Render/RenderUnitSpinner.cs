using System.Collections.Generic;
using System.Linq;
using OpenRa.Graphics;

namespace OpenRa.Traits
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
