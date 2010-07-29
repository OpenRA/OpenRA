#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Traits;
using System.Drawing;

namespace OpenRA.Mods.RA
{
	public class AttackBaseInfo : ITraitInfo
	{
		[WeaponReference]
		public readonly string PrimaryWeapon = null;
		[WeaponReference]
		public readonly string SecondaryWeapon = null;
		public readonly int Recoil = 0;
		public readonly int[] PrimaryLocalOffset = { };
		public readonly int[] SecondaryLocalOffset = { };
		public readonly int[] PrimaryOffset = { 0, 0 };
		public readonly int[] SecondaryOffset = null;
		public readonly bool MuzzleFlash = false;
		public readonly int FireDelay = 0;

		public virtual object Create(ActorInitializer init) { return new AttackBase(init.self); }
	}

	public class AttackBase : IIssueOrder, IResolveOrder, ITick, IExplodeModifier, IOrderCursor, IOrderVoice
	{
		public Target target;

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
			if (!target.IsValid) return false;
			if ((primaryFireDelay > 0) && (secondaryFireDelay > 0)) return false;
			if (self.traits.WithInterface<IDisable>().Any(d => d.Disabled)) return false;

			return true;
		}

		public bool ShouldExplode(Actor self) { return !IsReloading(); }

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

			if (weapon.Range * weapon.Range * Game.CellSize * Game.CellSize
			    < (target.CenterLocation - self.CenterLocation).LengthSquared) return false;
			
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
				fireDelay = weapon.BurstDelay;
			else
			{
				fireDelay = weapon.ROF;
				burst = weapon.Burst;
			}

			var destUnit = target.IsActor ? target.Actor.traits.GetOrDefault<Unit>() : null;

			var args = new ProjectileArgs
			{
				weapon = Rules.Weapons[weaponName.ToLowerInvariant()],

				firedBy = self,
				target = this.target,

				src = self.CenterLocation.ToInt2() + Combat.GetTurretPosition(self, unit, fireOffset, 0f).ToInt2(),
				srcAltitude = unit != null ? unit.Altitude : 0,
				dest = target.CenterLocation.ToInt2(),
				destAltitude = destUnit != null ? destUnit.Altitude : 0,
				
				facing = thisLocalOffset.ElementAtOrDefault(2) +
					(self.traits.Contains<Turreted>() ? self.traits.Get<Turreted>().turretFacing :
					unit != null ? unit.Facing : Util.GetFacing(target.CenterLocation - self.CenterLocation, 0)),
			};
			
			ScheduleDelayedAction( FireDelay( self, self.Info.Traits.Get<AttackBaseInfo>() ), () =>
			{
				if (args.weapon.Projectile != null)
				{
					var projectile = args.weapon.Projectile.Create(args);
					if (projectile != null)
						self.World.Add(projectile);

					if (!string.IsNullOrEmpty(args.weapon.Report))
						Sound.Play(args.weapon.Report + ".aud", self.CenterLocation);
				}
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
			if (mi.Button == MouseButton.Left) return null;
			if (self == underCursor) return null;

			var target = underCursor == null ? Target.FromCell(xy) : Target.FromActor(underCursor);

			var isHeal = self.GetPrimaryWeapon().Warheads.First().Damage < 0;
			var forceFire = mi.Modifiers.HasModifier(Modifiers.Ctrl);

			if (isHeal)
			{
				// we can never "heal ground"; that makes no sense.
				if (!target.IsActor) return null;
				
				// unless forced, only heal allies.
				if (self.Owner.Stances[underCursor.Owner] != Stance.Ally && !forceFire) return null;
				
				// we can only heal actors with health
				var health = underCursor.traits.GetOrDefault<Health>();
				if (health == null) return null;
				
				// don't allow healing of fully-healed stuff!
				if (health.HP >= health.MaxHP) return null;
			}
			else
			{
				if (!target.IsActor)
				{
					if (!forceFire) return null;
					return new Order("Attack", self, xy);
				}

				if ((self.Owner.Stances[underCursor.Owner] != Stance.Enemy) && !forceFire)
					return null;
			}
			
			if (!Combat.HasAnyValidWeapons(self, target)) return null;

			return new Order(isHeal ? "Heal" : "Attack", self, underCursor);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Attack" || order.OrderString == "Heal")
			{
				self.CancelActivity();
				QueueAttack(self, order);

				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (order.TargetActor != null)
							w.Add(new FlashTarget(order.TargetActor));
						
						var line = self.traits.GetOrDefault<DrawLineToTarget>();
						if (line != null)
							if (order.TargetActor != null) line.SetTarget(self, Target.FromOrder(order), Color.Red);
							else line.SetTarget(self, Target.FromOrder(order), Color.Red);
					});
			}
			else
				target = Target.None;
		}

		public string CursorForOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "Attack": return "attack";
				case "Heal": return "heal";
				default: return null;
			}
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Attack" || order.OrderString == "Heal") ? "Attack" : null;
		}
		
		protected virtual void QueueAttack(Actor self, Order order)
		{
			/* todo: choose the appropriate weapon, when only one works against this target */
			var weapon = self.GetPrimaryWeapon() ?? self.GetSecondaryWeapon();
			self.QueueActivity(
				new Activities.Attack(
					Target.FromOrder(order), 
					Math.Max(0, (int)weapon.Range)));
		}
	}
}
