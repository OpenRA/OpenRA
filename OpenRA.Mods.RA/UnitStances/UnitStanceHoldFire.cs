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
			: base(self, info)
		{

		}

		public override string OrderString
		{
			get { return "StanceHoldFire"; }
		}

		protected override void OnActivate(Actor self)
		{
			if (!self.HasTrait<AttackBase>()) return;

			if (self.Trait<AttackBase>().IsAttacking)
				StopAttack(self);
		}

		public override Color SelectionColor
		{
			get { return Color.SpringGreen; }
		}

		protected override string Shape
		{
			get { return " xx \nxxxx\n xx "; }
		}
	}
}