using System;
using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class UnitStanceReturnFireInfo : UnitStanceInfo
	{
		public override object Create(ActorInitializer init) { return new UnitStanceReturnFire(init.self, this); }
	}

	/// <summary>
	/// Return Fire
	/// 
	/// Will fire only when fired upon
	/// </summary>
	public class UnitStanceReturnFire : UnitStance, INotifyDamage, ISelectionColorModifier
	{
		public UnitStanceReturnFire(Actor self, UnitStanceReturnFireInfo info)
			: base(self, info)
		{
	
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!Active) return;
			if (!self.HasTrait<AttackBase>()) return;

			ReturnFire(self, e, false); // only triggers when standing still
		}

		public override string OrderString
		{
			get { return "StanceReturnFire"; }
		}

		public override Color SelectionColor
		{
			get { return Color.Orange; }
		}
		protected override string Shape
		{
			get { return "xxx\nxxx\nxxx\n x \n x \n\nxxx \nxxx "; }
		}
	}
}