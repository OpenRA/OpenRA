#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Test
{
	sealed class HealthTestObserver(string name, List<string> events) : INotifyDamage, INotifyDamageStateChanged, INotifyKilled
	{
		public readonly List<AttackInfo> Deaths = [];
		public Action<AttackInfo> OnDamage;
		public Action<AttackInfo> OnStateChanged;
		public Action<AttackInfo> OnKilled;

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			events.Add(name + "-damage");
			OnDamage?.Invoke(e);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			events.Add(name + "-state");
			OnStateChanged?.Invoke(e);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			events.Add(name + "-killed");
			Deaths.Add(e);
			OnKilled?.Invoke(e);
		}
	}

	[TestFixture]
	sealed class HealthTest
	{
		Health health;
		HealthTestObserver actor;
		HealthTestObserver player;
		List<string> events;

		[SetUp]
		public void SetUp()
		{
			var info = new HealthInfo();
			FieldLoader.LoadFieldOrProperty(info, nameof(HealthInfo.HP), "100");
			health = new Health(new ActorInitializer(null, []), info) { RemoveOnDeath = false };
			events = [];
			actor = new HealthTestObserver("actor", events);
			player = new HealthTestObserver("player", events);

			// Install the caches normally populated by Created, without constructing a world.
			var caches = new Dictionary<string, Array>
			{
				{ "notifyDamage", new INotifyDamage[] { actor } },
				{ "notifyDamagePlayer", new INotifyDamage[] { player } },
				{ "notifyDamageStateChanged", new INotifyDamageStateChanged[] { actor } },
				{ "notifyKilled", new INotifyKilled[] { actor } },
				{ "notifyKilledPlayer", new INotifyKilled[] { player } },
				{ "damageModifiers", Array.Empty<IDamageModifier>() },
				{ "damageModifiersPlayer", Array.Empty<IDamageModifier>() },
			};

			foreach (var cache in caches)
				typeof(Health).GetField(cache.Key, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(health, cache.Value);
		}

		void AssertDeathCount(int expected)
		{
			Assert.That(actor.Deaths, Has.Count.EqualTo(expected));
			Assert.That(player.Deaths, Has.Count.EqualTo(expected));
		}

		void KillFromDamage(AttackInfo e)
		{
			if (e.Damage.Value == 1)
				health.Kill(null, null, default);
		}

		[TestCase(100)]
		[TestCase(200)]
		public void OrdinaryDeathRetainsNotificationOrder(int damage)
		{
			health.InflictDamage(null, null, new Damage(damage), true);
			AssertDeathCount(1);
			Assert.That(health.IsDead, Is.True);
			Assert.That(actor.Deaths[0].Damage.Value, Is.EqualTo(damage));
			Assert.That(events, Is.EqualTo(new[] { "actor-damage", "player-damage", "actor-state", "actor-killed", "player-killed" }));
		}

		[TestCase(false)]
		[TestCase(true)]
		public void NestedLethalDamageNotifiesDeathOnce(bool fromPlayer)
		{
			(fromPlayer ? player : actor).OnDamage = KillFromDamage;
			health.InflictDamage(null, null, new Damage(1), true);
			AssertDeathCount(1);
			Assert.That(actor.Deaths[0].Damage.Value, Is.EqualTo(100));
			Assert.That(actor.Deaths[0].DamageState, Is.EqualTo(DamageState.Dead));
		}

		[Test]
		public void NestedDeathFromStateNotificationIsNotRepeated()
		{
			actor.OnStateChanged = e =>
			{
				if (!health.IsDead)
					health.Kill(null, null, default);
			};
			health.InflictDamage(null, null, new Damage(1), true);
			AssertDeathCount(1);
		}

		[Test]
		public void NestedHealingDoesNotNotifyDeath()
		{
			actor.OnDamage = e =>
			{
				if (e.Damage.Value == 1)
					health.InflictDamage(null, null, new Damage(-100), true);
			};
			health.InflictDamage(null, null, new Damage(1), true);
			Assert.That(health.HP, Is.EqualTo(100));
			AssertDeathCount(0);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void ResurrectionAllowsAnotherDeath(bool nested)
		{
			if (nested)
				actor.OnDamage = KillFromDamage;

			health.InflictDamage(null, null, new Damage(nested ? 1 : 100), true);
			health.Resurrect(null, null);
			Assert.That(health.HP, Is.EqualTo(100));
			health.InflictDamage(null, null, new Damage(nested ? 1 : 100), true);
			AssertDeathCount(2);
		}

		[Test]
		public void ResurrectionDuringDamageCancelsDeath()
		{
			actor.OnDamage = e =>
			{
				if (health.IsDead)
					health.Resurrect(null, null);
			};
			health.Kill(null, null, default);
			Assert.That(health.IsDead, Is.False);
			AssertDeathCount(0);
			actor.OnDamage = null;
			health.Kill(null, null, default);
			AssertDeathCount(1);
		}

		[Test]
		public void ResurrectionFromKilledAllowsLaterDeath()
		{
			actor.OnKilled = e =>
			{
				if (actor.Deaths.Count == 1)
					health.Resurrect(null, null);
			};
			health.Kill(null, null, default);
			Assert.That(health.IsDead, Is.False);
			health.Kill(null, null, default);
			AssertDeathCount(2);
		}

		[Test]
		public void TwoLivesWithinNestedDamageEachNotifyOnce()
		{
			actor.OnDamage = KillFromDamage;
			player.OnDamage = KillFromDamage;
			actor.OnKilled = e =>
			{
				if (actor.Deaths.Count == 1)
					health.Resurrect(null, null);
			};
			health.InflictDamage(null, null, new Damage(1), true);
			AssertDeathCount(2);
			Assert.That(health.IsDead, Is.True);
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(-100)]
		public void DamageToDeadActorDoesNotRepeatNotifications(int damage)
		{
			health.Kill(null, null, default);
			var notificationCount = events.Count;
			health.InflictDamage(null, null, new Damage(damage), true);
			AssertDeathCount(1);
			Assert.That(events, Has.Count.EqualTo(notificationCount));
		}
	}
}
