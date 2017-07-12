#region Copyright & License Information
/*
 * By Boolbada of OP Mod
 * Follows OpenRA's license as follows:
 *
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/* Works without base engine modification
 * ACCUMULATES condition per shot.
 * Useful for: Natasha/Boris airstrike and Gattling.
 */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	public class GrantStackableConditionOnFireInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition type to grant.")]
		public readonly string Condition = null;

		[WeaponReference]
		[Desc("Name of the armament that grants this condition.")]
		public readonly string ArmamentName = null;

		[Desc("Shots required to apply an instance of the condition. If there could be more instances than values listed,",
			"the last value is used for all following instances beyond the defined range.")]
		public readonly int[] RequiredShotsPerStack = { 3 };

		[Desc("Maximum instances of the stackable condition.")]
		public readonly int MaximumInstances = 2;

		[Desc("Should all instances reset if the actor passes the final stage?")]
		public readonly bool IsCyclic = false;

		[Desc("Amount of ticks required to pass without firing to revoke an instance.")]
		public readonly int RevokeDelay = 15;

		[Desc("Should an instance be revoked if the actor changes target?")]
		public readonly bool RevokeOnNewTarget = false;

		[Desc("Should all instances be revoked when any one of them are revoked?")]
		public readonly bool RevokeAll = false;

		public object Create(ActorInitializer init) { return new GrantStackableConditionOnFire(init, this); }
	}

	public class GrantStackableConditionOnFire : INotifyCreated, ITick, INotifyAttack
	{
		readonly GrantStackableConditionOnFireInfo info;
		readonly Stack<int> tokens = new Stack<int>();

		int cooldown = 0;
		int shotsFired = 0;
		ConditionManager manager;
		Target lastTarget = Target.Invalid;

		public GrantStackableConditionOnFire(ActorInitializer init, GrantStackableConditionOnFireInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();
		}

		void StackCondition(Actor self, string cond)
		{
			if (manager == null)
				return;

			if (string.IsNullOrEmpty(cond))
				return;

			tokens.Push(manager.GrantCondition(self, cond));
		}

		void UnstackCondition(Actor self, bool revokeAll)
		{
			shotsFired = 0;

			if (manager == null)
				return;

			if (tokens.Count == 0)
				return;

			if (!revokeAll)
				manager.RevokeCondition(self, tokens.Pop());
			else
				while (tokens.Count > 0)
					manager.RevokeCondition(self, tokens.Pop());
		}

		public void Tick(Actor self)
		{
			if (tokens.Count > 0 && --cooldown == 0)
			{
				cooldown = info.RevokeDelay;
				UnstackCondition(self, info.RevokeAll);
			}
		}

		bool TargetChanged(Target lastTarget, Target target)
		{
			// Invalidate reveal changing the target.
			if (lastTarget.Type == TargetType.FrozenActor && target.Type == TargetType.Actor)
				if (lastTarget.FrozenActor.Actor == target.Actor)
					return false;

			if (lastTarget.Type == TargetType.Actor && target.Type == TargetType.FrozenActor)
				if (target.FrozenActor.Actor == lastTarget.Actor)
					return false;

			if (lastTarget.Type != target.Type)
				return true;

			// Invalidate attacking different targets with shared target types.
			if (lastTarget.Type == TargetType.Actor && target.Type == TargetType.Actor)
				if (lastTarget.Actor != target.Actor)
					return true;

			if (lastTarget.Type == TargetType.FrozenActor && target.Type == TargetType.FrozenActor)
				if (lastTarget.FrozenActor != target.FrozenActor)
					return true;

			if (lastTarget.Type == TargetType.Terrain && target.Type == TargetType.Terrain)
				if (lastTarget.CenterPosition != target.CenterPosition)
					return true;

			return false;
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (a.Info.Name != info.ArmamentName)
				return;

			if (info.RevokeOnNewTarget)
			{
				if (TargetChanged(lastTarget, target))
					UnstackCondition(self, info.RevokeAll);

				lastTarget = target;
			}

			cooldown = info.RevokeDelay;

			if (!info.IsCyclic && tokens.Count >= info.MaximumInstances)
				return;

			shotsFired++;
			var requiredShots = tokens.Count < info.RequiredShotsPerStack.Length
				? info.RequiredShotsPerStack[tokens.Count]
				: info.RequiredShotsPerStack[info.RequiredShotsPerStack.Length - 1];

			if (shotsFired >= requiredShots)
			{
				if (info.IsCyclic && tokens.Count == info.MaximumInstances)
					UnstackCondition(self, true);
				else
					StackCondition(self, info.Condition);

				shotsFired = 0;
			}
		}

		public void PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }
	}
}