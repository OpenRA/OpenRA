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
		{
			Info = info;
			Active = (self.World.LocalPlayer == self.Owner || (self.Owner.IsBot && Game.IsHost)) ? Info.Default : false;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!Active) return;
			if (!self.HasTrait<AttackBase>()) return;

			ReturnFire(self, e, false); // only triggers when standing still
		}

		public Color GetSelectionColorModifier(Actor self, Color defaultColor)
		{
			return Active ? Color.Orange : defaultColor;
		}
	}
}