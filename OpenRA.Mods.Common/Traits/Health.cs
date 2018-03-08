#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class HealthInfo : ITraitInfo, UsesInit<HealthInit>, IRulesetLoaded
	{
		[Desc("HitPoints")]
		public readonly int HP = 0;
		[Desc("Trigger interfaces such as AnnounceOnKill?")]
		public readonly bool NotifyAppliedDamage = true;

		public virtual object Create(ActorInitializer init) { return new Health(init, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!ai.HasTraitInfo<HitShapeInfo>())
				throw new YamlException("Actors with Health need at least one HitShape trait!");
		}
	}

	public class Health : IHealth, ISync, ITick
	{
		public readonly HealthInfo Info;

		[Sync] int hp;

		public int DisplayHP { get; private set; }

		public Health(ActorInitializer init, HealthInfo info)
		{
			Info = info;
			MaxHP = info.HP > 0 ? info.HP : 1;

			// Cast to long to avoid overflow when multiplying by the health
			hp = init.Contains<HealthInit>() ? (int)(init.Get<HealthInit, int>() * (long)MaxHP / 100) : MaxHP;

			DisplayHP = hp;
		}

		public int HP { get { return hp; } }
		public int MaxHP { get; private set; }

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

		public void Resurrect(Actor self, Actor repairer)
		{
			if (!IsDead)
				return;

			hp = MaxHP;

			var ai = new AttackInfo
			{
				Attacker = repairer,
				Damage = new Damage(-MaxHP),
				DamageState = DamageState,
				PreviousDamageState = DamageState.Dead,
			};

			foreach (var nd in self.TraitsImplementing<INotifyDamage>()
					.Concat(self.Owner.PlayerActor.TraitsImplementing<INotifyDamage>()))
				nd.Damaged(self, ai);

			foreach (var nd in self.TraitsImplementing<INotifyDamageStateChanged>())
				nd.DamageStateChanged(self, ai);

			if (Info.NotifyAppliedDamage && repairer != null && repairer.IsInWorld && !repairer.IsDead)
				foreach (var nd in repairer.TraitsImplementing<INotifyAppliedDamage>()
						.Concat(repairer.Owner.PlayerActor.TraitsImplementing<INotifyAppliedDamage>()))
					nd.AppliedDamage(repairer, self, ai);
		}

		public void InflictDamage(Actor self, Actor attacker, Damage damage, bool ignoreModifiers)
		{
			// Overkill! Don't count extra hits as more kills!
			if (IsDead)
				return;

			var oldState = DamageState;

			// Apply any damage modifiers
			if (!ignoreModifiers && damage.Value > 0)
			{
				var modifiers = self.TraitsImplementing<IDamageModifier>()
					.Concat(self.Owner.PlayerActor.TraitsImplementing<IDamageModifier>())
					.Select(t => t.GetDamageModifier(attacker, damage));

				damage = new Damage(Util.ApplyPercentageModifiers(damage.Value, modifiers), damage.DamageTypes);
			}

			hp = (hp - damage.Value).Clamp(0, MaxHP);

			var ai = new AttackInfo
			{
				Attacker = attacker,
				Damage = damage,
				DamageState = DamageState,
				PreviousDamageState = oldState,
			};

			foreach (var nd in self.TraitsImplementing<INotifyDamage>()
					.Concat(self.Owner.PlayerActor.TraitsImplementing<INotifyDamage>()))
				nd.Damaged(self, ai);

			if (DamageState != oldState)
				foreach (var nd in self.TraitsImplementing<INotifyDamageStateChanged>())
					nd.DamageStateChanged(self, ai);

			if (Info.NotifyAppliedDamage && attacker != null && attacker.IsInWorld && !attacker.IsDead)
				foreach (var nd in attacker.TraitsImplementing<INotifyAppliedDamage>()
						.Concat(attacker.Owner.PlayerActor.TraitsImplementing<INotifyAppliedDamage>()))
					nd.AppliedDamage(attacker, self, ai);

			if (hp == 0)
			{
				foreach (var nd in self.TraitsImplementing<INotifyKilled>()
						.Concat(self.Owner.PlayerActor.TraitsImplementing<INotifyKilled>()))
					nd.Killed(self, ai);

				if (RemoveOnDeath)
					self.Dispose();

				if (attacker == null)
					Log.Write("debug", "{0} #{1} was killed.", self.Info.Name, self.ActorID);
				else
					Log.Write("debug", "{0} #{1} killed by {2} #{3}", self.Info.Name, self.ActorID, attacker.Info.Name, attacker.ActorID);
			}
		}

		public void Kill(Actor self, Actor attacker, HashSet<string> damageTypes = null)
		{
			if (damageTypes == null)
				damageTypes = new HashSet<string>();

			InflictDamage(self, attacker, new Damage(MaxHP, damageTypes), true);
		}

		void ITick.Tick(Actor self)
		{
			if (hp > DisplayHP)
				DisplayHP = hp;

			if (DisplayHP > hp)
				DisplayHP = (2 * DisplayHP + hp) / 3;
		}
	}

	public class HealthInit : IActorInit<int>
	{
		[FieldFromYamlKey] readonly int value = 100;
		readonly bool allowZero;
		public HealthInit() { }
		public HealthInit(int init)
			: this(init, false) { }

		public HealthInit(int init, bool allowZero)
		{
			this.allowZero = allowZero;
			value = init;
		}

		public int Value(World world)
		{
			if (value < 0 || (value == 0 && !allowZero))
				return 1;

			return value;
		}
	}
}
