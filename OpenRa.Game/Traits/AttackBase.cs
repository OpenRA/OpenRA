using System;
using System.Collections.Generic;
using System.Linq;
using IjwFramework.Types;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class AttackBaseInfo : ITraitInfo
	{
		public readonly string PrimaryWeapon = null;
		public readonly string SecondaryWeapon = null;
		public readonly int Recoil = 0;
		public readonly int[] PrimaryLocalOffset = { };
		public readonly int[] SecondaryLocalOffset = { };
		public readonly int[] PrimaryOffset = { 0, 0 };
		public readonly int[] SecondaryOffset = null;
		public readonly bool MuzzleFlash = false;
		public readonly int FireDelay = 0;

		public virtual object Create(Actor self) { return new AttackBase(self); }
	}

	class AttackBase : IIssueOrder, IResolveOrder, ITick
	{
		[Sync] public Actor target;

		// time (in frames) until each weapon can fire again.
		[Sync]
		protected int primaryFireDelay = 0;
		[Sync]
		protected int secondaryFireDelay = 0;

		int primaryBurst;
		int secondaryBurst;

		public float primaryRecoil = 0.0f, secondaryRecoil = 0.0f;

		public AttackBase(Actor self)
		{
			var info = self.Info.Traits.WithInterface<AttackBaseInfo>().First();

			var primaryWeapon = info.PrimaryWeapon != null ? Rules.WeaponInfo[info.PrimaryWeapon] : null;
			var secondaryWeapon = info.SecondaryWeapon != null ? Rules.WeaponInfo[info.SecondaryWeapon] : null;

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
			var info = self.Info.Traits.WithInterface<AttackBaseInfo>().First();

			if (info.PrimaryWeapon != null && CheckFire(self, unit, info.PrimaryWeapon, ref primaryFireDelay,
				info.PrimaryOffset, ref primaryBurst, info.PrimaryLocalOffset))
			{
				secondaryFireDelay = Math.Max(4, secondaryFireDelay);
				primaryRecoil = 1;
				return;
			}

			if (info.SecondaryWeapon != null && CheckFire(self, unit, info.SecondaryWeapon, ref secondaryFireDelay,
				info.SecondaryOffset ?? info.PrimaryOffset, ref secondaryBurst, info.SecondaryLocalOffset))
			{
				if (info.SecondaryOffset != null) secondaryRecoil = 1;
				else primaryRecoil = 1;
				return;
			}
		}

		bool CheckFire(Actor self, Unit unit, string weaponName, ref int fireDelay, int[] offset, ref int burst, int[] localOffset)
		{
			if (fireDelay > 0) return false;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return false;

			var weapon = Rules.WeaponInfo[weaponName];
			if (weapon.Range * weapon.Range < (target.Location - self.Location).LengthSquared) return false;

			if (!Combat.WeaponValidForTarget(weapon, target)) return false;

			var numOffsets = (localOffset.Length + 2) / 3;
			if (numOffsets == 0) numOffsets = 1;
			var localOffsetForShot = burst % numOffsets;
			var thisLocalOffset = localOffset.Skip(3 * localOffsetForShot).Take(3).ToArray();

			var fireOffset = new[] { 
				offset.ElementAtOrDefault(0) + thisLocalOffset.ElementAtOrDefault(0), 
				offset.ElementAtOrDefault(1) + thisLocalOffset.ElementAtOrDefault(1), 
				offset.ElementAtOrDefault(2),
				offset.ElementAtOrDefault(3) };

			if (--burst > 0)
				fireDelay = 5;
			else
			{
				fireDelay = weapon.ROF;
				burst = weapon.Burst;
			}

			var firePos = self.CenterLocation.ToInt2() + Util.GetTurretPosition(self, unit, fireOffset, 0f).ToInt2();
			var thisTarget = target;	// closure.
			var destUnit = thisTarget.traits.GetOrDefault<Unit>();
			var info = self.Info.Traits.WithInterface<AttackBaseInfo>().First();

			ScheduleDelayedAction(info.FireDelay, () =>
			{
				var srcAltitude = unit != null ? unit.Altitude : 0;
				var destAltitude = destUnit != null ? destUnit.Altitude : 0;

				if( weapon.RenderAsTesla )
					Game.world.Add( new TeslaZap( firePos, thisTarget.CenterLocation.ToInt2() ) );

				if (Rules.ProjectileInfo[weapon.Projectile].ROT != 0)
				{
					var fireFacing = thisLocalOffset.ElementAtOrDefault(2) + 
						(self.traits.Contains<Turreted>() ? self.traits.Get<Turreted>().turretFacing : unit.Facing);
	
					Game.world.Add(new Missile(weaponName, self.Owner, self,
						firePos, thisTarget, srcAltitude, fireFacing));
				}
				else
					Game.world.Add(new Bullet(weaponName, self.Owner, self,
						firePos, thisTarget.CenterLocation.ToInt2(), srcAltitude, destAltitude));

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

			var info = self.Info.Traits.WithInterface<AttackBaseInfo>().First();
			var isHeal = Rules.WeaponInfo[info.PrimaryWeapon].Damage < 0;
			if (((underCursor.Owner == self.Owner) ^ isHeal) 
				&& !mi.Modifiers.HasModifier( Modifiers.Ctrl )) return null;

			if (!Combat.HasAnyValidWeapons(self, underCursor)) return null;

			return new Order(isHeal ? "Heal" : "Attack", self, underCursor, int2.Zero, null);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Attack" || order.OrderString == "Heal")
			{
				self.CancelActivity();
				QueueAttack(self, order);

				if (self.Owner == Game.LocalPlayer)
					Game.world.AddFrameEndTask(w => w.Add(new FlashTarget(order.TargetActor)));
			}
			else
				target = null;
		}

		protected virtual void QueueAttack(Actor self, Order order)
		{
			var info = self.Info.Traits.WithInterface<AttackBaseInfo>().First();
			const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
			/* todo: choose the appropriate weapon, when only one works against this target */
			var weapon = info.PrimaryWeapon ?? info.SecondaryWeapon;

			self.QueueActivity(new Activities.Attack(order.TargetActor,
					Math.Max(0, (int)Rules.WeaponInfo[weapon].Range - RangeTolerance)));
		}
	}
}
