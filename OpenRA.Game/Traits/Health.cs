#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	public class HealthInfo : ITraitInfo
	{
		public readonly int HP = 0;
		public readonly float Radius = 10;
		public virtual object Create(ActorInitializer init) { return new Health(init, this); }
	}

	public enum DamageState { Undamaged, Light, Medium, Heavy, Critical, Dead };
	
	public class Health
	{
		public readonly HealthInfo Info;
		
		[Sync]
		int hp;
		
		public Health(ActorInitializer init, HealthInfo info)
		{
			Info = info;
			MaxHP = info.HP;
			hp = init.Contains<HealthInit>() ? (int)(init.Get<HealthInit, float>()*MaxHP) : MaxHP;
		}
		
		public int HP { get { return hp; } }
		public readonly int MaxHP;
		public float HPFraction
		{
			get { return hp * 1f / MaxHP; }
		}
		
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
		
		public void InflictDamage(Actor self, Actor attacker, int damage, WarheadInfo warhead)
		{
			if (IsDead) return;		/* overkill! don't count extra hits as more kills! */

			var oldState = this.DamageState;
			
			/* apply the damage modifiers, if we have any. */
            var modifier = (float)self.TraitsImplementing<IDamageModifier>().Concat(self.Owner.PlayerActor.TraitsImplementing<IDamageModifier>())
				.Select(t => t.GetDamageModifier(attacker, warhead)).Product();

			damage = (int)(damage * modifier);

			hp -= damage;

            foreach (var nd in self.TraitsImplementing<INotifyDamage>().Concat(self.Owner.PlayerActor.TraitsImplementing<INotifyDamage>()))
				nd.Damaged(self, new AttackInfo
				{
					Attacker = attacker,
					Damage = damage,
					DamageState = this.DamageState,
					PreviousDamageState = oldState,
					DamageStateChanged = this.DamageState != oldState,
					Warhead = warhead,
                    PreviousHealth = hp + damage < 0 ? 0 : hp + damage,
                    Health = hp
				});

			if (hp <= 0)
			{
				hp = 0;

				attacker.Owner.Kills++;
				self.Owner.Deaths++;

				if( RemoveOnDeath )
					self.Destroy();

				Log.Write("debug", "{0} #{1} killed by {2} #{3}", self.Info.Name, self.ActorID, attacker.Info.Name, attacker.ActorID);
			}

			if (hp > MaxHP)	hp = MaxHP;
		}
	}
	
	
	public class HealthInit : IActorInit<float>
	{
		[FieldFromYamlKey]
		public readonly float value = 1f;
		
		public HealthInit() { }
		
		public HealthInit( float init )
		{
			value = init;
		}
		
		public float Value( World world )
		{
			return value;	
		}
	}
	
	
	public static class HealthExts
	{
		public static bool IsDead(this Actor self)
		{
			if (self.Destroyed)	return true;
			
			var health = self.TraitOrDefault<Health>();
			return (health == null) ? true : health.IsDead;
		}
				
		public static DamageState GetDamageState(this Actor self)
		{
			var health = self.TraitOrDefault<Health>();
			return (health == null) ? DamageState.Undamaged : health.DamageState;
		}
		
		public static void InflictDamage(this Actor self, Actor attacker, int damage, WarheadInfo warhead)
		{
			var health = self.TraitOrDefault<Health>();
			if (health == null) return;
			health.InflictDamage(self, attacker, damage, warhead);
		}
		
		public static void Kill(this Actor self, Actor attacker)
		{
			var health = self.TraitOrDefault<Health>();
			if (health == null) return;

			/* hack. Fix for proper */
			health.InflictDamage(self, attacker, int.MaxValue, null);
		}
	}
}
