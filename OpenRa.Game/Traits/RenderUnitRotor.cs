using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitRotor : RenderUnit
	{
		public Animation rotorAnim, secondRotorAnim;

		public RenderUnitRotor( Actor self )
			: base(self)
		{
			var unit = self.traits.Get<Unit>();

			rotorAnim = new Animation(self.Info.Name);
			rotorAnim.PlayRepeating("rotor");
			anims.Add( "rotor_1", new AnimationWithOffset(
				rotorAnim,
				() => Util.GetTurretPosition( self, unit, self.Info.PrimaryOffset, 0 ),
				null ) );

			if( self.Info.SecondaryOffset == null ) return;

			secondRotorAnim = new Animation( self.Info.Name );
			secondRotorAnim.PlayRepeating( "rotor2" );
			anims.Add( "rotor_2", new AnimationWithOffset(
				secondRotorAnim,
				() => Util.GetTurretPosition( self, unit, self.Info.SecondaryOffset, 0 ),
				null ) );
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
