#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackTeslaInfo : AttackOmniInfo
	{
		public readonly int MaxCharges = 3;
		public readonly int ReloadTime = 120;
		public readonly bool Overchargeable = true;
		public readonly string OverchargeArm = "overcharge";
		public readonly int OverchargeReloadSpeedup = 5;
		public readonly int OverchargeRemovalTime = 125;

		public override object Create(ActorInitializer init) { return new AttackTesla(init.self, this); }
	}

	class AttackTesla : AttackOmni, ITick, INotifyAttack, ISync, INotifyDamage
	{
		[Sync] int charges;
		[Sync] int timeToRecharge;
		[Sync] int timeToOverchargeRemoval;

		readonly AttackTeslaInfo info;

		public bool Overchargeable { get { return info.Overchargeable; } }
		public bool Overcharged { get { return timeToOverchargeRemoval > 0; } }

		public AttackTesla(Actor self, AttackTeslaInfo info)
			: base(self)
		{
			this.info = info;
			charges = info.MaxCharges;
		}

		public override void Tick(Actor self)
		{
			if (--timeToRecharge <= 0)
				charges = info.MaxCharges;

			timeToOverchargeRemoval--;

			base.Tick(self);
		}

		public void Attacking(Actor self, Target target)
		{
			--charges;
			timeToRecharge = info.ReloadTime;
		}

		public override WRange GetMaximumRange()
		{
			return new WRange(1024 * (int)
				(Overcharged ? Armaments : Armaments.Where(a => a.Info.Name != info.OverchargeArm))
				.Select(a => a.Weapon.Range).Max());
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			return new TeslaAttack(newTarget);
		}

		public override void ResolveOrder(Actor self, Order order)
		{
			base.ResolveOrder(self, order);

			if (order.OrderString == "Stop")
				self.CancelActivity();
		}

		public override void DoAttack(Actor self, Target target)
		{
			if (!Overchargeable)
			{
				base.DoAttack(self, target);
				return;
			}

			if (!CanAttack(self, target))
				return;

			var overchargeArms = Armaments.Where(a => a.Info.Name == info.OverchargeArm);
			var primaryArms = Armaments.Where(a => a.Info.Name != info.OverchargeArm);

			var move = self.TraitOrDefault<IMove>();
			var facing = self.TraitOrDefault<IFacing>();

			if (Overcharged)
			{
				foreach (var arm in overchargeArms)
					arm.CheckFire(self, this, move, facing, target);
			}
			else
			{
				foreach (var arm in primaryArms)
					arm.CheckFire(self, this, move, facing, target);
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!e.Attacker.IsDead() && e.Attacker.HasTrait<AttackOvercharge>() && e.Attacker.Owner.IsAlliedWith(self.Owner))
			{
				timeToRecharge -= info.OverchargeReloadSpeedup;
				timeToOverchargeRemoval = info.OverchargeRemovalTime;
			}
		}

		class TeslaAttack : Activity
		{
			readonly Target target;
			public TeslaAttack(Target target) { this.target = target; }

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !target.IsValid) return NextActivity;

				var attack = self.Trait<AttackTesla>();
				if (attack.charges == 0 || !attack.CanAttack(self, target))
					return this;

				self.Trait<RenderBuildingCharge>().PlayCharge(self);
				return Util.SequenceActivities(new Wait(22), new TeslaZap(target), this);
			}
		}

		class TeslaZap : Activity
		{
			readonly Target target;
			public TeslaZap(Target target) { this.target = target; }

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !target.IsValid) return NextActivity;

				var attack = self.Trait<AttackTesla>();
				if (attack.charges == 0) return NextActivity;

				attack.DoAttack(self, target);

				return Util.SequenceActivities(new Wait(3), this);
			}
		}
	}

	class AttackOverchargeInfo : AttackFrontalInfo, ITraitInfo
	{
		public readonly string OverchargeArm = "overcharge";

		public override object Create(ActorInitializer init) { return new AttackOvercharge(init.self, this); }
	}

	class AttackOvercharge : AttackFrontal, IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly AttackOverchargeInfo info;

		public AttackOvercharge(Actor self, AttackOverchargeInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void DoAttack(Actor self, Target target)
		{
			if (!base.CanAttack(self, target))
				return;

			var overchargeArms = Armaments.Where(a => a.Info.Name == info.OverchargeArm);
			var primaryArms = Armaments.Where(a => a.Info.Name != info.OverchargeArm);

			var move = self.TraitOrDefault<IMove>();
			var facing = self.TraitOrDefault<IFacing>();

			var targetTesla = target.Actor.TraitOrDefault<AttackTesla>();

			if (targetTesla != null && targetTesla.Overchargeable && self.Owner.IsAlliedWith(target.Actor.Owner) && overchargeArms.Any())
			{
				foreach (var a in overchargeArms)
					a.CheckFire(self, this, move, facing, target);
			}
			else
			{
				foreach (var arm in primaryArms)
					arm.CheckFire(self, this, move, facing, target);
			}
		}

		public override IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new TargetTypeOrderTargeter("AttackOvercharge", "AttackOvercharge", 6, "ability", false, true); }
		}

		public override Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "AttackOvercharge")
				return new Order("AttackOvercharge", self, queued) { TargetActor = target.Actor };

			return null;
		}

		public override void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackOvercharge")
			{
				var target = Target.FromOrder(order);
				self.SetTargetLine(target, Color.Cyan);
				self.QueueActivity(false, new Attack(target, self.Trait<AttackBase>().GetMaximumRange()));
			}
		}

		public override string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "AttackOvercharge" ? "Attack" : null;
		}
	}
}
