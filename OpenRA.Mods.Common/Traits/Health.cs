#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.HitShapes;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class HealthInfo : ITraitInfo, UsesInit<HealthInit>
	{
		[Desc("HitPoints")]
		public readonly int HP = 0;
		[Desc("Trigger interfaces such as AnnounceOnKill?")]
		public readonly bool NotifyAppliedDamage = true;

		[ProvidedConditionReference]
		[Desc("Name of condition for checking health.")]
		public readonly string HealthCondition = null;

		[ProvidedConditionReference]
		[Desc("Name of condition for checking damage state.")]
		public readonly string DamageStateCondition = null;

		[FieldLoader.LoadUsing("LoadShape")]
		public readonly IHitShape Shape;

		static object LoadShape(MiniYaml yaml)
		{
			IHitShape ret;

			var shapeNode = yaml.Nodes.Find(n => n.Key == "Shape");
			var shape = shapeNode != null ? shapeNode.Value.Value : string.Empty;

			if (!string.IsNullOrEmpty(shape))
			{
				ret = Game.CreateObject<IHitShape>(shape + "Shape");

				try
				{
					FieldLoader.Load(ret, shapeNode.Value);
				}
				catch (YamlException e)
				{
					throw new YamlException("HitShape {0}: {1}".F(shape, e.Message));
				}
			}
			else
				ret = new CircleShape();

			ret.Initialize();
			return ret;
		}

		public virtual object Create(ActorInitializer init) { return new Health(init, this); }
	}

	class HealthCondition : NotifyingCondition
	{
		NumberCondition hpVariable;
		NumberCondition maxHpVariable;

		public int Value { get { return hpVariable.Value; } }

		public void Set(Actor self, int hp)
		{
				if (hpVariable.Value == hp)
					return;
				hpVariable.Value = hp;
				NotifyConditionChanged(self);
		}

		public HealthCondition(int hp, int max)
		{
			hpVariable = new NumberCondition(hp);
			maxHpVariable = new NumberCondition(max);
		}

		public override bool AsBool() { return hpVariable.Value > 0; }
		public override int AsInt() { return hpVariable.Value; }
		public override ICondition Get(string name)
		{
			switch (name)
			{
				case "current":	return hpVariable;
				case "max":	return maxHpVariable;
				case "isDead":
					return hpVariable.Value <= 0 ? BoolCondition.True : BoolCondition.False;
				case "alive":
					return hpVariable.Value > 0  ? BoolCondition.False : BoolCondition.True;
				default:	return EmptyCondition.Instance;
			}
		}
	}

	class DamageStateCondition : NotifyingCondition
	{
		static NumberCondition undamaged = new NumberCondition((int)DamageState.Undamaged);
		static NumberCondition light = new NumberCondition((int)DamageState.Light);
		static NumberCondition medium = new NumberCondition((int)DamageState.Medium);
		static NumberCondition heavy = new NumberCondition((int)DamageState.Heavy);
		static NumberCondition critical = new NumberCondition((int)DamageState.Critical);
		static NumberCondition dead = new NumberCondition((int)DamageState.Dead);
		DamageState state;

		public DamageState Value { get { return state; } }

		public DamageStateCondition(int hp, int max)
		{
			state = CalculateState(hp, max);
		}

		public void Set(Actor self, int hp, int max)
		{
			var current = CalculateState(hp, max);
			if (state == current)
				return;

			state = current;
			NotifyConditionChanged(self);
		}

		static DamageState CalculateState(int hp, int max)
		{
			if (hp <= 0)
				return DamageState.Dead;

			if (hp < max * 0.25f)
				return DamageState.Critical;

			if (hp < max * 0.5f)
				return DamageState.Heavy;

			if (hp < max * 0.75f)
				return DamageState.Medium;

			if (hp == max)
				return DamageState.Undamaged;

			return DamageState.Light;
		}

		public override bool AsBool() { return state != DamageState.Undamaged; }
		public override int AsInt() { return (int)state; }
		public override ICondition Get(string name)
		{
			switch (name)
			{
				case "noDamage":
					return state == DamageState.Undamaged ? BoolCondition.True : BoolCondition.False;
				case "isLight":
					return state == DamageState.Light ? BoolCondition.True : BoolCondition.False;
				case "isMedium":
					return state == DamageState.Medium ? BoolCondition.True : BoolCondition.False;
				case "isHeavy":
					return state == DamageState.Heavy ? BoolCondition.True : BoolCondition.False;
				case "isCritical":
					return state == DamageState.Critical ? BoolCondition.True : BoolCondition.False;
				case "isDead":
					return state == DamageState.Dead ? BoolCondition.True : BoolCondition.False;
				case "alive":
					return state == DamageState.Dead ? BoolCondition.False : BoolCondition.True;
				case "undamaged":	return undamaged;
				case "light":	return light;
				case "medium":	return medium;
				case "heavy":	return heavy;
				case "critical":	return critical;
				case "dead":	return dead;
				default:	return EmptyCondition.Instance;
			}
		}
	}

	public class Health : IHealth, ISync, ITick, INotifyingConditionProvider
	{
		public readonly HealthInfo Info;

		[Sync] int hp;

		public int DisplayHP { get; private set; }

		public Health(ActorInitializer init, HealthInfo info)
		{
			Info = info;
			MaxHP = info.HP > 0 ? info.HP : 1;

			hp = init.Contains<HealthInit>() ? init.Get<HealthInit, int>() * MaxHP / 100 : MaxHP;
			hpCondition = new HealthCondition(hp, MaxHP);
			damageCondition = new DamageStateCondition(hp, MaxHP);

			DisplayHP = hp;
		}

		public int HP { get { return hp; } }
		public int MaxHP { get; private set; }
		HealthCondition hpCondition;
		DamageStateCondition damageCondition;

		public bool IsDead { get { return hp <= 0; } }
		public bool RemoveOnDeath = true;

		public DamageState DamageState { get { return damageCondition.Value; } }

		IEnumerable<KeyValuePair<string, INotifyingCondition>> INotifyingConditionProvider.Provided
		{
			get
			{
				if (!string.IsNullOrEmpty(Info.HealthCondition))
					yield return new KeyValuePair<string, INotifyingCondition>(Info.HealthCondition, hpCondition);
				if (!string.IsNullOrEmpty(Info.DamageStateCondition))
					yield return new KeyValuePair<string, INotifyingCondition>(Info.DamageStateCondition, damageCondition);
			}
		}

		public void Resurrect(Actor self, Actor repairer)
		{
			if (!IsDead)
				return;

			hp = MaxHP;
			hpCondition.Set(self, hp);
			damageCondition.Set(self, hp, MaxHP);

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
			hpCondition.Set(self, hp);
			damageCondition.Set(self, hp, MaxHP);

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

		public void Kill(Actor self, Actor attacker)
		{
			InflictDamage(self, attacker, new Damage(MaxHP), true);
		}

		public void Tick(Actor self)
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
