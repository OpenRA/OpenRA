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
using OpenRA.Traits.Activities;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	public class HealthInfo : ITraitInfo
	{
		public readonly int HP = 0;
		public readonly int InitialHP = 0;
		public readonly ArmorType Armor = ArmorType.none;		
		public virtual object Create(ActorInitializer init) { return new Health(init, this); }
	}

	public enum ExtendedDamageState { Normal, ThreeQuarter, Half, Quarter, Dead };
	
	public class Health
	{
		public readonly HealthInfo Info;
		
		[Sync]
		int hp;
		
		public Health(ActorInitializer init, HealthInfo info)
		{
			Info = info;
			MaxHP = info.HP;
			hp = (info.InitialHP == 0) ? MaxHP : info.InitialHP;
		}
		
		public int HP { get { return hp; } }
		public readonly int MaxHP;
		public float HPFraction { get { return hp*1f/MaxHP; } }
		
		public bool IsDead { get { return hp <= 0; } }
		public bool RemoveOnDeath = true;
		
		public DamageState DamageState
		{
			get
			{
				if (hp <= 0)
					return DamageState.Dead;
				
				if (hp < MaxHP * 0.5f)
					return DamageState.Half;
	
				return DamageState.Normal;
			}
		}
		
		public ExtendedDamageState ExtendedDamageState
		{
			get 
			{
				if (hp <= 0)
					return ExtendedDamageState.Dead;
	
				if (hp < MaxHP * 0.25f)
					return ExtendedDamageState.Quarter;
	
				if (hp < MaxHP * 0.5f)
					return ExtendedDamageState.Half;
	
				if (hp < MaxHP * 0.75f)
					return ExtendedDamageState.ThreeQuarter;
	
				return ExtendedDamageState.Normal;
			}
		}
		
		public void InflictDamage(Actor self, Actor attacker, int damage, WarheadInfo warhead)
		{
			if (IsDead) return;		/* overkill! don't count extra hits as more kills! */

			var oldState = this.DamageState;
			var oldExtendedState = this.ExtendedDamageState;
			
			/* apply the damage modifiers, if we have any. */
			var modifier = (float)self.traits.WithInterface<IDamageModifier>()
				.Select(t => t.GetDamageModifier(warhead)).Product();

			damage = (int)(damage * modifier);

			hp -= damage;
			if (hp <= 0)
			{
				hp = 0;

				attacker.Owner.Kills++;
				self.Owner.Deaths++;

				if (RemoveOnDeath)
					self.World.AddFrameEndTask(w => w.Remove(self));

				Log.Write("debug", "{0} #{1} killed by {2} #{3}", self.Info.Name, self.ActorID, attacker.Info.Name, attacker.ActorID);
			}

			if (hp > MaxHP)	hp = MaxHP;

			foreach (var nd in self.traits.WithInterface<INotifyDamage>())
				nd.Damaged(self, new AttackInfo
				{
					Attacker = attacker,
					Damage = damage,
					DamageState = this.DamageState,
					ExtendedDamageState = this.ExtendedDamageState,
					DamageStateChanged = this.DamageState != oldState,
					ExtendedDamageStateChanged = this.ExtendedDamageState != oldExtendedState,
					Warhead = warhead
				});
		}

		public void TransferHPFromActor(Actor self, Actor from, bool transferPercentage)
		{
			var fromHealth = from.traits.GetOrDefault<Health>();
			if (fromHealth == null)
				return;
						
			hp = (transferPercentage) ? (int)(fromHealth.HPFraction*MaxHP) : Math.Min(fromHealth.HP, MaxHP);
		}
	}
	
	public static class HealthExts
	{
		public static int Health(this Actor self)
		{
			var health = self.traits.GetOrDefault<Health>();
			return (health == null) ? 0 : health.HP;
		}
		
		public static bool IsDead(this Actor self)
		{
			var health = self.traits.GetOrDefault<Health>();
			return (health == null) ? true : health.IsDead;
		}
		
		public static DamageState GetDamageState(this Actor self)
		{
			var health = self.traits.GetOrDefault<Health>();
			return (health == null) ? DamageState.Normal : health.DamageState;
		}
		
		public static void InflictDamage(this Actor self, Actor attacker, int damage, WarheadInfo warhead)
		{
			var health = self.traits.GetOrDefault<Health>();
			if (health == null) return;
			health.InflictDamage(self, attacker, damage, warhead);
		}
		
		public static void Kill(this Actor self, Actor attacker)
		{
			var health = self.traits.GetOrDefault<Health>();
			if (health == null) return;
			health.InflictDamage(self, attacker, health.HP, null);
		}
	}
}
