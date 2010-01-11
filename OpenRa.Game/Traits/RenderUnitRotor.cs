using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
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
				null ) );

			if (info.SecondaryOffset == null) return;

			secondRotorAnim = new Animation(GetImage(self));
			secondRotorAnim.PlayRepeating( "rotor2" );
			anims.Add( "rotor_2", new AnimationWithOffset(
				secondRotorAnim,
				() => Util.GetTurretPosition(self, unit, info.SecondaryOffset, 0),
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
