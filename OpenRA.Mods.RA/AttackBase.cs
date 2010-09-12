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
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Traits;

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

		public readonly bool AlignIdleTurrets = false;

		public virtual object Create(ActorInitializer init) { return new AttackBase(init.self); }
	}

	public class AttackBase : IIssueOrder, IResolveOrder, ITick, IExplodeModifier, IOrderCursor, IOrderVoice
	{
		public Target target;

		public List<Weapon> Weapons = new List<Weapon>();
		public List<Turret> Turrets = new List<Turret>();

		public AttackBase(Actor self)
		{
			var info = self.Info.Traits.Get<AttackBaseInfo>();

			Turrets.Add(new Turret(info.PrimaryOffset));
			if (info.SecondaryOffset != null)
				Turrets.Add(new Turret(info.SecondaryOffset));

			if (info.PrimaryWeapon != null)
				Weapons.Add(new Weapon(info.PrimaryWeapon, 
					Turrets[0], info.PrimaryLocalOffset));

			if (info.SecondaryWeapon != null)
				Weapons.Add(new Weapon(info.SecondaryWeapon, 
					info.SecondaryOffset != null ? Turrets[1] : Turrets[0], info.SecondaryLocalOffset));
		}

		protected virtual bool CanAttack(Actor self)
		{
			if (!target.IsValid) return false;
			if (Weapons.All(w => w.IsReloading)) return false;
			if (self.TraitsImplementing<IDisable>().Any(d => d.Disabled)) return false;

			return true;
		}

		public bool ShouldExplode(Actor self) { return !IsReloading(); }

		public bool IsReloading() { return Weapons.Any(w => w.IsReloading); }

		List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		public virtual void Tick(Actor self)
		{
			foreach (var w in Weapons)
				w.Tick();

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

			var move = self.TraitOrDefault<IMove>();
			var facing = self.TraitOrDefault<IFacing>();
			foreach (var w in Weapons)
				if (CheckFire(self, move, facing, w))
					w.FiredShot();
		}

		bool CheckFire(Actor self, IMove move, IFacing facing, Weapon w)
		{		
			if (w.FireDelay > 0) return false;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return false;

			if (w.Info.Range * w.Info.Range * Game.CellSize * Game.CellSize
			    < (target.CenterLocation - self.CenterLocation).LengthSquared) return false;
			
			if (!w.IsValidAgainst(target)) return false;

			var barrel = w.Barrels[w.Burst % w.Barrels.Length];
			var destMove = target.IsActor ? target.Actor.TraitOrDefault<IMove>() : null;

			var args = new ProjectileArgs
			{
				weapon = w.Info,

				firedBy = self,
				target = this.target,

				src = (self.CenterLocation
					+ Combat.GetTurretPosition(self, facing, w.Turret)
					+ Combat.GetBarrelPosition(self, facing, w.Turret, barrel)).ToInt2(),
				srcAltitude = move != null ? move.Altitude : 0,
				dest = target.CenterLocation.ToInt2(),
				destAltitude = destMove != null ? destMove.Altitude : 0,
				
				facing = barrel.Facing + 
					(self.HasTrait<Turreted>() ? self.Trait<Turreted>().turretFacing :
					facing != null ? facing.Facing : Util.GetFacing(target.CenterLocation - self.CenterLocation, 0)),

				firepowerModifier = self.TraitsImplementing<IFirepowerModifier>()
					 .Select(a => a.GetFirepowerModifier())
					 .Product()
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

			foreach (var na in self.TraitsImplementing<INotifyAttack>())
				na.Attacking(self);

			return true;
		}

		public virtual int FireDelay( Actor self, AttackBaseInfo info )
		{
			return info.FireDelay;
		}

		public int OrderPriority(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return mi.Modifiers.HasModifier(Modifiers.Ctrl) ? 1000 : 1;
		}
		
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;
			if (self == underCursor) return null;

			var target = underCursor == null ? Target.FromCell(xy) : Target.FromActor(underCursor);

			var isHeal = Weapons[0].Info.Warheads[0].Damage < 0;
			var forceFire = mi.Modifiers.HasModifier(Modifiers.Ctrl);

			if (isHeal)
			{
				// we can never "heal ground"; that makes no sense.
				if (!target.IsActor) return null;
				
				// unless forced, only heal allies.
				if (self.Owner.Stances[underCursor.Owner] != Stance.Ally && !forceFire) return null;
				
				// don't allow healing of fully-healed stuff!
				if (underCursor.GetDamageState() == DamageState.Undamaged) return null;
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
			
			if (!HasAnyValidWeapons(target)) return null;

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

						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							if (order.TargetActor != null) line.SetTarget(self, Target.FromOrder(order), Color.Red);
							else line.SetTarget(self, Target.FromOrder(order), Color.Red);
					});
			}
			else
			{
				target = Target.None;

				/* hack */
				if (self.HasTrait<Turreted>() && self.Info.Traits.Get<AttackBaseInfo>().AlignIdleTurrets)
					self.Trait<Turreted>().desiredFacing = null;
			}
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
			var weapon = ChooseWeaponForTarget(Target.FromOrder(order));

			if (weapon != null)
				self.QueueActivity(
					new Activities.Attack(
						Target.FromOrder(order), 
						Math.Max(0, (int)weapon.Info.Range)));
		}

		public bool HasAnyValidWeapons(Target t) { return Weapons.Any(w => w.IsValidAgainst(t)); }
		public float GetMaximumRange() { return Weapons.Max(w => w.Info.Range); }

		public Weapon ChooseWeaponForTarget(Target t) { return Weapons.FirstOrDefault(w => w.IsValidAgainst(t)); }
	}
}
