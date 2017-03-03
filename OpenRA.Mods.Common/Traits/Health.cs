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

	class DamageStateCondition : NotifyingCondition
	{
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
		public override int GetAsInt(string name) { return GetAsInt(name, true); }
		public override bool GetAsBool(string name) { return GetAsBool(name, true); }

		int GetAsInt(string name, bool resultType)
		{
			switch (name)
			{
				case "undamaged":	return (int)DamageState.Undamaged;
				case "light":	return (int)DamageState.Light;
				case "medium":	return (int)DamageState.Medium;
				case "heavy":	return (int)DamageState.Heavy;
				case "critical":	return (int)DamageState.Critical;
				case "dead":	return (int)DamageState.Dead;
				default:
					return resultType && GetAsBool(name, false) ? 1 : 0;
			}
		}

		bool GetAsBool(string name, bool resultType)
		{
			switch (name)
			{
				case "noDamage":
					return state == DamageState.Undamaged;
				case "isLight":
					return state == DamageState.Light;
				case "isMedium":
					return state == DamageState.Medium;
				case "isHeavy":
					return state == DamageState.Heavy;
				case "isCritical":
					return state == DamageState.Critical;
				case "isDead":
					return state == DamageState.Dead;
				case "alive":
					return state != DamageState.Dead;
				default:
					return resultType && GetAsInt(name, false) != 0;
			}
		}
	}

	public class Health : NotifyingCondition, IHealth, ISync, ITick, INotifyingConditionProvider
	{
		public readonly HealthInfo Info;

		[Sync] int hp;

		public int DisplayHP { get; private set; }

		public Health(ActorInitializer init, HealthInfo info)
		{
			Info = info;
			MaxHP = info.HP > 0 ? info.HP : 1;

			hp = init.Contains<HealthInit>() ? init.Get<HealthInit, int>() * MaxHP / 100 : MaxHP;
			damageCondition = new DamageStateCondition(hp, MaxHP);

			DisplayHP = hp;
		}

		public int HP { get { return hp; } }
		public int MaxHP { get; private set; }
		DamageStateCondition damageCondition;

		public bool IsDead { get { return hp <= 0; } }
		public bool RemoveOnDeath = true;

		public DamageState DamageState { get { return damageCondition.Value; } }

		IEnumerable<KeyValuePair<string, INotifyingCondition>> INotifyingConditionProvider.Provided
		{
			get
			{
				if (!string.IsNullOrEmpty(Info.HealthCondition))
					yield return new KeyValuePair<string, INotifyingCondition>(Info.HealthCondition, this);
				if (!string.IsNullOrEmpty(Info.DamageStateCondition))
					yield return new KeyValuePair<string, INotifyingCondition>(Info.DamageStateCondition, damageCondition);
			}
		}

		public void Resurrect(Actor self, Actor repairer)
		{
			if (!IsDead)
				return;

			hp = MaxHP;
			NotifyConditionChanged(self);
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
			NotifyConditionChanged(self);
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

		public override bool AsBool() { return hp > 0; }
		public override int AsInt() { return hp; }
		public override int GetAsInt(string name) { return GetAsInt(name, true); }
		public override bool GetAsBool(string name) { return GetAsBool(name, true); }
		int GetAsInt(string name, bool resultType)
		{
			switch (name)
			{
				case "current":	return hp;
				case "max":	return MaxHP;
				default:	return resultType && GetAsBool(name, false) ? 1 : 0;
			}
		}

		bool GetAsBool(string name, bool resultType)
		{
			switch (name)
			{
				case "isDead":	return hp <= 0;
				case "alive":	return hp > 0;
				default:	return resultType && GetAsInt(name, false) != 0;
			}
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
