using System;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class AttackBase : IOrder, ITick
	{
		public Actor target;

		// time (in frames) until each weapon can fire again.
		protected int primaryFireDelay = 0;
		protected int secondaryFireDelay = 0;

		public float primaryRecoil = 0.0f, secondaryRecoil = 0.0f;

		public AttackBase(Actor self) { }

		protected bool CanAttack(Actor self)
		{
			return target != null;
		}

		public bool IsReloading()
		{
			return (primaryFireDelay > 0) || (secondaryFireDelay > 0);
		}

		public virtual void Tick(Actor self)
		{
			if (primaryFireDelay > 0) --primaryFireDelay;
			if (secondaryFireDelay > 0) --secondaryFireDelay;

			primaryRecoil = Math.Max(0f, primaryRecoil - .2f);
			secondaryRecoil = Math.Max(0f, secondaryRecoil - .2f);

			if (target != null && target.IsDead) target = null;		/* he's dead, jim. */
		}

		public void DoAttack(Actor self)
		{
			var unit = self.traits.Get<Unit>();

			if (self.Info.Primary != null && CheckFire(self, unit, self.Info.Primary, ref primaryFireDelay,
				self.Info.PrimaryOffset))
			{
				secondaryFireDelay = Math.Max(4, secondaryFireDelay);
				primaryRecoil = 1;
				return;
			}

			if (self.Info.Secondary != null && CheckFire(self, unit, self.Info.Secondary, ref secondaryFireDelay,
				self.Info.SecondaryOffset ?? self.Info.PrimaryOffset))
			{
				if (self.Info.SecondaryOffset != null) secondaryRecoil = 1;
				else primaryRecoil = 1;
				return;
			}
		}

		bool CheckFire(Actor self, Unit unit, string weaponName, ref int fireDelay, int[] offset)
		{
			if (fireDelay > 0) return false;
			var weapon = Rules.WeaponInfo[weaponName];
			if (weapon.Range * weapon.Range < (target.Location - self.Location).LengthSquared) return false;

			if (!Combat.WeaponValidForTarget(weapon, target)) return false;

			fireDelay = weapon.ROF;
			var projectile = Rules.ProjectileInfo[weapon.Projectile];

			var firePos = self.CenterLocation.ToInt2() + Util.GetTurretPosition(self, unit, offset, 0f).ToInt2();

			if (projectile.ROT != 0)
				Game.world.Add(new Missile(weaponName, self.Owner, self,
					firePos, target));
			else
				Game.world.Add(new Bullet(weaponName, self.Owner, self,
					firePos, target.CenterLocation.ToInt2()));

			return true;
		}

		public Order IssueOrder(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if (lmb || underCursor == null) return null;
			if (underCursor.Owner == self.Owner) return null;
			return Order.Attack(self, underCursor);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Attack")
			{
				self.CancelActivity();
				QueueAttack(self, order);
			}
		}

		protected virtual void QueueAttack(Actor self, Order order)
		{
			const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
			/* todo: choose the appropriate weapon, when only one works against this target */
			var weapon = order.Subject.Info.Primary ?? order.Subject.Info.Secondary;

			self.QueueActivity(new Traits.Activities.Attack(order.TargetActor,
					Math.Max(0, (int)Rules.WeaponInfo[weapon].Range - RangeTolerance)));
		}
	}
}
