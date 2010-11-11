using System;
using System.Drawing;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class UnitStanceHoldGroundInfo : UnitStanceInfo
	{
		public override object Create(ActorInitializer init) { return new UnitStanceHoldGround(init.self, this); }
	}

	public class UnitStanceHoldGround : UnitStance, INotifyDamage
	{
		public UnitStanceHoldGround(Actor self, UnitStanceHoldGroundInfo info)
			: base(self, info)
		{
		
		}


		public override string OrderString
		{
			get { return "StanceHoldGround"; }
		}

		protected override void OnScan(Actor self)
		{
			if (!self.IsIdle) return;
			if (!self.HasTrait<AttackBase>()) return;

			var target = ScanForTarget(self);
			if (target == null)
				return;

			AttackTarget(self, target, true);
		}

		protected override void OnActivate(Actor self)
		{
			if (!self.HasTrait<AttackBase>()) return;

			if (self.Trait<AttackBase>().IsAttacking)
				StopAttack(self);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!Active) return;
			if (!self.HasTrait<AttackBase>()) return;

			ReturnFire(self, e, false, false, true); // only triggers when standing still
		}

		public override Color SelectionColor
		{
			get { return Color.Yellow; }
		}

		protected override string Shape
		{
			get { return "xxx\nx x\nx x\nxxx"; }
		}
	}
}