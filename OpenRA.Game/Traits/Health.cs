#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	public class HealthInfo : ITraitInfo, UsesInit<HealthInit>
	{
		public readonly int HP = 0;
		public readonly float Radius = 10;
		public virtual object Create(ActorInitializer init) { return new Health(init, this); }
	}

	public enum DamageState { Undamaged, Light, Medium, Heavy, Critical, Dead };

	public class Health : ISync, ITick
	{
		public readonly HealthInfo Info;

		[Sync] int hp;

		public int DisplayHp { get; private set; }

		public Health(ActorInitializer init, HealthInfo info)
		{
			Info = info;
			MaxHP = info.HP;

			hp = init.Contains<HealthInit>() ? (int)(init.Get<HealthInit, float>() * MaxHP) : MaxHP;
			DisplayHp = hp;
		}

		public int HP { get { return hp; } }
		public int MaxHP;

		public bool IsDead { get { return hp <= 0; } }
		public bool RemoveOnDeath = true;

		public DamageState DamageState
		{
			get
			{
				if (hp <= 0)
					return DamageState.Dead;

				if (hp < MaxHP * 0.25f)
					return DamageState.Critical;

				if (hp < MaxHP * 0.5f)
					return DamageState.Heavy;

				if (hp < MaxHP * 0.75f)
					return DamageState.Medium;

				if (hp == MaxHP)
					return DamageState.Undamaged;

				return DamageState.Light;
			}
		}

		public void InflictDamage(Actor self, Actor attacker, int damage, WarheadInfo warhead, bool ignoreModifiers)
		{
			if (IsDead) return;		/* overkill! don't count extra hits as more kills! */

			var oldState = this.DamageState;
			/* apply the damage modifiers, if we have any. */
			var modifier = (float)self.TraitsImplementing<IDamageModifier>()
				.Concat(self.Owner.PlayerActor.TraitsImplementing<IDamageModifier>())
				.Select(t => t.GetDamageModifier(attacker, warhead)).Product();

			if (!ignoreModifiers)
				damage = damage > 0 ? (int)(damage * modifier) : damage;

			hp = Exts.Clamp(hp - damage, 0, MaxHP);

			var ai = new AttackInfo
			{
				Attacker = attacker,
				Damage = damage,
				DamageState = this.DamageState,
				PreviousDamageState = oldState,
				Warhead = warhead,
			};

			foreach (var nd in self.TraitsImplementing<INotifyDamage>()
					 .Concat(self.Owner.PlayerActor.TraitsImplementing<INotifyDamage>()))
				nd.Damaged(self, ai);

			if (DamageState != oldState)
				foreach (var nd in self.TraitsImplementing<INotifyDamageStateChanged>())
					nd.DamageStateChanged(self, ai);

			if (attacker != null && attacker.IsInWorld && !attacker.IsDead())
				foreach (var nd in attacker.TraitsImplementing<INotifyAppliedDamage>()
					 .Concat(attacker.Owner.PlayerActor.TraitsImplementing<INotifyAppliedDamage>()))
				nd.AppliedDamage(attacker, self, ai);

			if (hp == 0)
			{
				attacker.Owner.Kills++;
				self.Owner.Deaths++;

				foreach (var nd in self.TraitsImplementing<INotifyKilled>()
						.Concat(self.Owner.PlayerActor.TraitsImplementing<INotifyKilled>()))
					nd.Killed(self, ai);

				if( RemoveOnDeath )
					self.Destroy();

				Log.Write("debug", "{0} #{1} killed by {2} #{3}", self.Info.Name, self.ActorID, attacker.Info.Name, attacker.ActorID);
			}
		}

		public void Tick(Actor self)
		{
			if (hp > DisplayHp)
				DisplayHp = hp;

			if (DisplayHp > hp)
				DisplayHp = (2 * DisplayHp + hp) / 3;
		}
	}

	public class HealthInit : IActorInit<float>
	{
		[FieldFromYamlKey] public readonly float value = 1f;
		public HealthInit() { }
		public HealthInit( float init ) { value = init; }
		public float Value( World world ) { return value; }
	}

	public static class HealthExts
	{
		public static bool IsDead(this Actor self)
		{
			if (self.Destroyed)	return true;

			var health = self.TraitOrDefault<Health>();
			return (health == null) ? false : health.IsDead;
		}

		public static DamageState GetDamageState(this Actor self)
		{
			if (self.Destroyed) return DamageState.Dead;

			var health = self.TraitOrDefault<Health>();
			return (health == null) ? DamageState.Undamaged : health.DamageState;
		}

		public static void InflictDamage(this Actor self, Actor attacker, int damage, WarheadInfo warhead)
		{
			var health = self.TraitOrDefault<Health>();
			if (health == null) return;
			health.InflictDamage(self, attacker, damage, warhead, false);
		}

		public static void Kill(this Actor self, Actor attacker)
		{
			var health = self.TraitOrDefault<Health>();
			if (health == null) return;
			health.InflictDamage(self, attacker, health.MaxHP, null, true);
		}
	}
}
