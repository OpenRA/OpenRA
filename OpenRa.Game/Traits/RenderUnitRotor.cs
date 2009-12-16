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
			rotorAnim = new Animation(self.Info.Name);
			rotorAnim.PlayRepeating("rotor");

			if (self.Info.SecondaryAnim != null)
			{
				secondRotorAnim = new Animation(self.Info.Name);
				secondRotorAnim.PlayRepeating(self.Info.SecondaryAnim);
			}
		}

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var unit = self.traits.Get<Unit>();

			yield return Util.CenteredShadow(self, anim.Image, self.CenterLocation);
			yield return Util.CenteredShadow(self, rotorAnim.Image, self.CenterLocation
				+ Util.GetTurretPosition(self, unit, self.Info.PrimaryOffset, 0));
			if (self.Info.SecondaryOffset != null)
				yield return Util.CenteredShadow(self, (secondRotorAnim ?? rotorAnim).Image, self.CenterLocation
					+ Util.GetTurretPosition(self, unit, self.Info.SecondaryOffset, 0));

			var p = self.CenterLocation - new float2( 0, unit.Altitude );

			yield return Util.Centered(self, anim.Image, p);
			yield return Util.Centered(self, rotorAnim.Image, p
				+ Util.GetTurretPosition( self, unit, self.Info.PrimaryOffset, 0 ) );
			if (self.Info.SecondaryOffset != null)
				yield return Util.Centered(self, (secondRotorAnim ?? rotorAnim).Image, p
					+ Util.GetTurretPosition( self, unit, self.Info.SecondaryOffset, 0 ) );
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			rotorAnim.Tick();
			if (secondRotorAnim != null)
				secondRotorAnim.Tick();

			var unit = self.traits.Get<Unit>();
			
			var isFlying = unit.Altitude > 0;

			if (isFlying ^ (rotorAnim.CurrentSequence.Name != "rotor")) 
				return;

			rotorAnim.PlayRepeatingPreservingPosition(isFlying ? "rotor" : "slow-rotor");
			if (secondRotorAnim != null)
				secondRotorAnim.PlayRepeatingPreservingPosition(isFlying ? "rotor2" : "slow-rotor2");
		}
	}
}
