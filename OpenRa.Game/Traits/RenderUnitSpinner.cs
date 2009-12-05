using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitSpinner : RenderUnit
	{
		public Animation spinnerAnim;

		public RenderUnitSpinner( Actor self )
			: base(self)
		{
			spinnerAnim = new Animation( self.Info.Name );
			spinnerAnim.PlayRepeating( "spinner" );
		}

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var unit = self.traits.Get<Unit>();

			yield return Util.Centered(self, anim.Image, self.CenterLocation);
			yield return Util.Centered( self, spinnerAnim.Image, self.CenterLocation 
				+ Util.GetTurretPosition(self, unit, self.Info.PrimaryOffset, 0));
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			spinnerAnim.Tick();
		}
	}
}
