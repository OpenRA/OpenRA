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

			yield return Util.Centered(self, anim.Image, self.CenterLocation);
			yield return Util.Centered(self, rotorAnim.Image, self.CenterLocation
				+ Util.GetTurretPosition( self, unit, self.Info.PrimaryOffset, 0 ) );
			if (self.Info.SecondaryOffset != null)
				yield return Util.Centered(self, (secondRotorAnim ?? rotorAnim).Image, self.CenterLocation
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

			rotorAnim.ReplaceAnim(isFlying ? "rotor" : "slow-rotor");
			if (secondRotorAnim != null)
				secondRotorAnim.ReplaceAnim(isFlying ? "rotor2" : "slow-rotor2");
		}
	}
}
