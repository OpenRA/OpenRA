#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	public class AttackBaseInfo : ITraitInfo
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
			var primaryWeapon = self.GetPrimaryWeapon();
			var secondaryWeapon = self.GetSecondaryWeapon();

			primaryBurst = primaryWeapon != null ? primaryWeapon.Burst : 1;
			secondaryBurst = secondaryWeapon != null ? secondaryWeapon.Burst : 1;
		}

		protected virtual bool CanAttack(Actor self)
		{
			if( target == null ) return false;
			if( ( primaryFireDelay > 0 ) && ( secondaryFireDelay > 0 ) ) return false;

			return true;
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
			if( !CanAttack( self ) ) return;

			var unit = self.traits.GetOrDefault<Unit>();
			var info = self.Info.Traits.Get<AttackBaseInfo>();

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

			var weapon = Rules.Weapons[weaponName.ToLowerInvariant()];
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

			var destUnit = target.traits.GetOrDefault<Unit>();

			var args = new ProjectileArgs
			{
				weapon = Rules.Weapons[weaponName.ToLowerInvariant()],

				firedBy = self,
				target = target,

				src = self.CenterLocation.ToInt2() + Util.GetTurretPosition(self, unit, fireOffset, 0f).ToInt2(),
				srcAltitude = unit != null ? unit.Altitude : 0,
				dest = target.CenterLocation.ToInt2(),
				destAltitude = destUnit != null ? destUnit.Altitude : 0,
				
				facing = thisLocalOffset.ElementAtOrDefault(2) +
					(self.traits.Contains<Turreted>() ? self.traits.Get<Turreted>().turretFacing :
					unit != null ? unit.Facing : Util.GetFacing(target.CenterLocation - self.CenterLocation, 0)),
			};
			
			ScheduleDelayedAction( FireDelay( self, self.Info.Traits.Get<AttackBaseInfo>() ), () =>
			{
				var projectile = args.weapon.Projectile.Create(args);
				if (projectile != null)
					self.World.Add(projectile);

				if (!string.IsNullOrEmpty(args.weapon.Report))
					Sound.Play(args.weapon.Report + ".aud");
			});

			foreach (var na in self.traits.WithInterface<INotifyAttack>())
				na.Attacking(self);

			return true;
		}

		public virtual int FireDelay( Actor self, AttackBaseInfo info )
		{
			return info.FireDelay;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left || underCursor == null || underCursor.Owner == null) return null;
			if (self == underCursor) return null;

			var isHeal = self.GetPrimaryWeapon().Warheads.First().Damage < 0;
			var forceFire = mi.Modifiers.HasModifier(Modifiers.Ctrl);

			if (isHeal)
			{
				if (underCursor.Owner == null)
					return null;
				if (self.Owner.Stances[ underCursor.Owner ] != Stance.Ally && !forceFire)
					return null;
				if (underCursor.Health >= underCursor.GetMaxHP())
					return null;	// don't allow healing of fully-healed stuff!
			}
			else
				if ((self.Owner.Stances[ underCursor.Owner ] != Stance.Enemy) && !forceFire)
					return null;
			
			if (!Combat.HasAnyValidWeapons(self, underCursor)) return null;

			return new Order(isHeal ? "Heal" : "Attack", self, underCursor);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Attack" || order.OrderString == "Heal")
			{
				self.CancelActivity();
				QueueAttack(self, order);

				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w => w.Add(new FlashTarget(order.TargetActor)));
			}
			else
				target = null;
		}

		protected virtual void QueueAttack(Actor self, Order order)
		{
			const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
			/* todo: choose the appropriate weapon, when only one works against this target */
			var weapon = self.GetPrimaryWeapon() ?? self.GetSecondaryWeapon();

			self.QueueActivity(new Activities.Attack(order.TargetActor,
					Math.Max(0, (int)weapon.Range - RangeTolerance)));
		}
	}
}
