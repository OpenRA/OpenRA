using System;
using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class UnitStanceHoldFireInfo : UnitStanceInfo
	{
		public override object Create(ActorInitializer init) { return new UnitStanceHoldFire(init.self, this); }
	}

	/// <summary>
	/// Hold Fire
	/// 
	/// Will not perform any attacks automaticly 
	/// </summary>
	public class UnitStanceHoldFire : UnitStance, ISelectionColorModifier
	{
		public UnitStanceHoldFire(Actor self, UnitStanceHoldFireInfo info)
		{
			Info = info;
			Active = (self.World.LocalPlayer == self.Owner || (self.Owner.IsBot && Game.IsHost)) ? Info.Default : false;
		}

		protected override void OnFirstTick(Actor self)
		{
			if (!self.HasTrait<AttackBase>()) return;

			if (self.Trait<AttackBase>().IsAttacking)
				StopAttack(self);
		}

		public Color GetSelectionColorModifier(Actor self, Color defaultColor)
		{
			return Active ? Color.SpringGreen : defaultColor;
		}
	}
}