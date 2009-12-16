using System;
using System.Collections.Generic;
using IjwFramework.Types;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class AttackBase : IOrder, ITick
	{
		public Actor target;

		// time (in frames) until each weapon can fire again.
		protected int primaryFireDelay = 0;
		protected int secondaryFireDelay = 0;

		int primaryBurst;
		int secondaryBurst;

		public float primaryRecoil = 0.0f, secondaryRecoil = 0.0f;

		public AttackBase(Actor self)
		{
			var primaryWeapon = self.Info.Primary != null ? Rules.WeaponInfo[self.Info.Primary] : null;
			var secondaryWeapon = self.Info.Secondary != null ? Rules.WeaponInfo[self.Info.Secondary] : null;

			primaryBurst = primaryWeapon != null ? primaryWeapon.Burst : 1;
			secondaryBurst = secondaryWeapon != null ? secondaryWeapon.Burst : 1;
		}

		protected bool CanAttack(Actor self)
		{
			return target != null;
		}

		public bool IsReloading()
		{
			return (primaryFireDelay > 0) || (secondaryFireDelay > 0);
		}

		List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		public virtual void Tick(Actor self)
		{
			if (primaryFireDelay > 0) --primaryFireDelay;
			if (secondaryFireDelay > 0) --secondaryFireDelay;

			primaryRecoil = Math.Max(0f, primaryRecoil - .2f);
			secondaryRecoil = Math.Max(0f, secondaryRecoil - .2f);

			if (target != null && target.IsDead) target = null;		/* he's dead, jim. */

			for (var i = 0; i < delayedActions.Count; i++)
			{
				var x = delayedActions[i];
				if (--x.First <= 0)
					x.Second();
				delayedActions[i] = x;
			}
			delayedActions.RemoveAll(a => a.First <= 0);
		}

		void ScheduleDelayedAction(int t, Action a)
		{
			if (t > 0)
				delayedActions.Add(Pair.New(t, a));
			else
				a();
		}

		public void DoAttack(Actor self)
		{
			var unit = self.traits.GetOrDefault<Unit>();

			if (self.Info.Primary != null && CheckFire(self, unit, self.Info.Primary, ref primaryFireDelay,
				self.Info.PrimaryOffset, ref primaryBurst))
			{
				secondaryFireDelay = Math.Max(4, secondaryFireDelay);
				primaryRecoil = 1;
				return;
			}

			if (self.Info.Secondary != null && CheckFire(self, unit, self.Info.Secondary, ref secondaryFireDelay,
				self.Info.SecondaryOffset ?? self.Info.PrimaryOffset, ref secondaryBurst))
			{
				if (self.Info.SecondaryOffset != null) secondaryRecoil = 1;
				else primaryRecoil = 1;
				return;
			}
		}

		bool CheckFire(Actor self, Unit unit, string weaponName, ref int fireDelay, int[] offset, ref int burst)
		{
			if (fireDelay > 0) return false;
			var weapon = Rules.WeaponInfo[weaponName];
			if (weapon.Range * weapon.Range < (target.Location - self.Location).LengthSquared) return false;

			if (!Combat.WeaponValidForTarget(weapon, target)) return false;

			if (--burst > 0)
				fireDelay = 5;
			else
			{
				fireDelay = weapon.ROF;
				burst = weapon.Burst;
			}

			var firePos = self.CenterLocation.ToInt2() + Util.GetTurretPosition(self, unit, offset, 0f).ToInt2();
			var thisTarget = target;
			ScheduleDelayedAction(self.Info.FireDelay, () =>
			{
				if( weapon.RenderAsTesla )
					Game.world.Add( new TeslaZap( firePos, thisTarget.CenterLocation.ToInt2() ) );

				if( Rules.ProjectileInfo[ weapon.Projectile ].ROT != 0 )
					Game.world.Add(new Missile(weaponName, self.Owner, self,
						firePos, thisTarget));
				else
					Game.world.Add(new Bullet(weaponName, self.Owner, self,
						firePos, thisTarget.CenterLocation.ToInt2()));

				if (!string.IsNullOrEmpty(weapon.Report))
					Sound.Play(weapon.Report + ".aud");
			});

			foreach (var na in self.traits.WithInterface<INotifyAttack>())
				na.Attacking(self);

			return true;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left || underCursor == null) return null;
			if (self == underCursor) return null;
			if (underCursor.Owner == self.Owner && !mi.Modifiers.HasModifier( Modifiers.Ctrl )) return null;
			if (!Combat.HasAnyValidWeapons(self, underCursor)) return null;
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

			self.QueueActivity(new Activities.Attack(order.TargetActor,
					Math.Max(0, (int)Rules.WeaponInfo[weapon].Range - RangeTolerance)));
		}
	}
}
