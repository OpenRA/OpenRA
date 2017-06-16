#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/*
 * Works without base engine modification.
 * Mindcontroller is assumed that they aren't mindcontrollable!
 */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	public enum MindcontrolPolicy
	{
		NewOneUnaffected, // Like Yuri's MC tower. Best if you use ControllingCondition to forbid the dummy weapon from firing too.
		DiscardOldest, // Like Yuri Clone
		HyperControl // Like Yuri Master Mind
	}

	// No permanent MC support though, I think it is better to make a separate module based on this.
	// All you need to do is to delete all complex code and leave ownership transfer code only.
	[Desc("Can mind control other units?")]
	public class MindcontrollerInfo : ConditionalTraitInfo, Requires<ArmamentInfo>, Requires<HealthInfo>
	{
		[WeaponReference]
		[Desc("The name of the weapon, one of its armament. Must be specified with \"Name:\" field.",
			"To limit mind controllable targets, adjust the weapon's valid target filter.")]
		public readonly string Name = "primary";

		[Desc("Up to how many units can this unit control?")]
		public readonly int Capacity = 1;

		[Desc("Can this unit MC beyond Capacity temporarily?")]
		public readonly MindcontrolPolicy Policy = MindcontrolPolicy.DiscardOldest;

		[Desc("Condition to grant to the controlled actor")]
		[GrantedConditionReference]
		public readonly string GiveCondition;

		[Desc("Condition to grant to self when controlling actors. Can stack up by the number of enslaved actors. You can use this to forbid firing of the dummy MC weapon.")]
		[GrantedConditionReference]
		public readonly string ControllingCondition;

		[Desc("Damage taken if hyper controlling beyond capacity.")]
		public readonly int HyperControlDamage = 2;

		[Desc("Interval of applying hyper control damage")]
		public readonly int HyperControlDamageInterval = 25;

		[Desc("The sound played when the unit is mindcontrolled.")]
		public readonly string[] Sound = null;

		[Desc("PipType to use for indicating MC'ed units")]
		public readonly PipType PipType = PipType.Yellow;

		[Desc("PipType to use for indicating left over MC capacity")]
		public readonly PipType PipTypeEmpty = PipType.Transparent;

		public override object Create(ActorInitializer init) { return new Mindcontroller(init.Self, this); }
	}

	class Mindcontroller : ConditionalTrait<MindcontrollerInfo>, INotifyAttack, IPips, INotifyKilled, INotifyActorDisposing, ITick,
			INotifyCreated
	{
		readonly MindcontrollerInfo info;
		readonly Health health;
		readonly List<Actor> slaves = new List<Actor>();

		int ticks;
		Stack<int> controllingTokens = new Stack<int>();
		ConditionManager conditionManager;

		public IEnumerable<Actor> Slaves { get { return slaves; } }

		public Mindcontroller(Actor self, MindcontrollerInfo info)
			: base(info)
		{
			this.info = info;
			health = self.Trait<Health>();

			var armaments = self.TraitsImplementing<Armament>().Where(a => a.Info.Name == info.Name).ToArray();
			System.Diagnostics.Debug.Assert(armaments.Length == 1, "Multiple armaments with given name detected: " + info.Name);
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void StackControllingCondition(Actor self, string condition)
		{
			if (conditionManager == null)
				return;

			if (string.IsNullOrEmpty(condition))
				return;

			controllingTokens.Push(conditionManager.GrantCondition(self, condition));
		}

		void UnstackControllingCondition(Actor self, string condition)
		{
			if (conditionManager == null)
				return;

			if (string.IsNullOrEmpty(condition))
				return;

			conditionManager.RevokeCondition(self, controllingTokens.Pop());
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			for (int i = slaves.Count(); i > 0; i--)
				yield return info.PipType;

			for (int i = info.Capacity - slaves.Count(); i > 0; i--)
				yield return info.PipTypeEmpty;
		}

		// Unlink a dead or mind-controlled-by-somebody-else slave.
		public void UnlinkSlave(Actor self, Actor slave)
		{
			UnstackControllingCondition(self, info.ControllingCondition);
			if (slaves.Contains(slave))
				slaves.Remove(slave);
		}

		public void PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{
			// Do nothing
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			// Only specified MC weapon can do mind control.
			if (info.Name != a.Info.Name)
				return;

			// Must target an actor.
			if (target.Actor == null || !target.IsValidFor(self))
				return;

			// Don't allow ally mind control
			if (self.Owner.Stances[target.Actor.Owner] == Stance.Ally)
				return;

			var mcable = target.Actor.TraitOrDefault<Mindcontrollable>();

			// For some reason the weapon is valid for targeting but the actor doesn't actually have
			// mindcontrollable trait.
			if (mcable == null)
			{
				Game.Debug("Warning: mindcontrollable unit doesn't actually have mindcontrallable trait");
				return;
			}

			if (info.Policy == MindcontrolPolicy.NewOneUnaffected && slaves.Count() >= info.Capacity)
				return;

			// At this point, the target should be mind controlled. How we manage them is another thing.
			slaves.Add(target.Actor);
			mcable.LinkMaster(target.Actor, self, info.GiveCondition);
			StackControllingCondition(self, info.ControllingCondition);

			// Play sound
			if (info.Sound != null && info.Sound.Any())
				Game.Sound.Play(SoundType.World, info.Sound.Random(self.World.SharedRandom), self.CenterPosition);

			// Let's evict the oldest one, if no hyper control.
			if (info.Policy == MindcontrolPolicy.DiscardOldest && slaves.Count() > info.Capacity)
				slaves[0].Trait<Mindcontrollable>().UnMindcontrol(slaves[0], self.Owner);

			// If can hyper control, nothing to do.
			// Tick() will do the rest.
		}

		void ReleaseSlaves(Actor self)
		{
			var toUnMC = slaves.ToArray(); // UnMincdontrol modifies slaves list.
			foreach (var s in toUnMC)
			{
				if (s.IsDead || s.Disposed)
					continue;
				s.Trait<Mindcontrollable>().UnMindcontrol(s, self.Owner);
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			ReleaseSlaves(self);
		}

		public void Disposing(Actor self)
		{
			ReleaseSlaves(self);
		}

		public void Tick(Actor self)
		{
			if (info.Policy != MindcontrolPolicy.HyperControl)
				return;

			if (slaves.Count() <= info.Capacity)
				return;

			if (ticks-- > 0)
				return;

			ticks = info.HyperControlDamageInterval;
			health.InflictDamage(self, self, new Damage(info.HyperControlDamage), true);
		}
	}
}