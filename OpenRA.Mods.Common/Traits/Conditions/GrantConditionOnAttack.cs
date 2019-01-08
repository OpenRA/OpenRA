#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class GrantConditionOnAttackInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition type to grant.")]
		public readonly string Condition = null;

		[Desc("Name of the armaments that grant this condition.")]
		public readonly HashSet<string> ArmamentNames = new HashSet<string>() { "primary" };

		[Desc("Shots required to apply an instance of the condition. If there are more instances of the condition granted than values listed,",
			"the last value is used for all following instances beyond the defined range.")]
		public readonly int[] RequiredShotsPerInstance = { 1 };

		[Desc("Maximum instances of the condition to grant.")]
		public readonly int MaximumInstances = 1;

		[Desc("Should all instances reset if the actor passes the final stage?")]
		public readonly bool IsCyclic = false;

		[Desc("Amount of ticks required to pass without firing to revoke an instance.")]
		public readonly int RevokeDelay = 15;

		[Desc("Should an instance be revoked if the actor changes target?")]
		public readonly bool RevokeOnNewTarget = false;

		[Desc("Should all instances be revoked instead of only one?")]
		public readonly bool RevokeAll = false;

		public object Create(ActorInitializer init) { return new GrantConditionOnAttack(init, this); }
	}

	public class GrantConditionOnAttack : INotifyCreated, ITick, INotifyAttack
	{
		readonly GrantConditionOnAttackInfo info;
		readonly Stack<int> tokens = new Stack<int>();

		int cooldown = 0;
		int shotsFired = 0;
		ConditionManager manager;

		// Only tracked when RevokeOnNewTarget is true.
		Target lastTarget = Target.Invalid;

		public GrantConditionOnAttack(ActorInitializer init, GrantConditionOnAttackInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.TraitOrDefault<ConditionManager>();
		}

		void GrantInstance(Actor self, string cond)
		{
			if (manager == null || string.IsNullOrEmpty(cond))
				return;

			tokens.Push(manager.GrantCondition(self, cond));
		}

		void RevokeInstance(Actor self, bool revokeAll)
		{
			shotsFired = 0;

			if (manager == null || tokens.Count == 0)
				return;

			if (!revokeAll)
				manager.RevokeCondition(self, tokens.Pop());
			else
				while (tokens.Count > 0)
					manager.RevokeCondition(self, tokens.Pop());
		}

		void ITick.Tick(Actor self)
		{
			if (tokens.Count > 0 && --cooldown == 0)
			{
				cooldown = info.RevokeDelay;
				RevokeInstance(self, info.RevokeAll);
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

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (!info.ArmamentNames.Contains(a.Info.Name))
				return;

			if (info.RevokeOnNewTarget)
			{
				if (TargetChanged(lastTarget, target))
					RevokeInstance(self, info.RevokeAll);

				lastTarget = target;
			}

			cooldown = info.RevokeDelay;

			if (!info.IsCyclic && tokens.Count >= info.MaximumInstances)
				return;

			shotsFired++;
			var requiredShots = tokens.Count < info.RequiredShotsPerInstance.Length
				? info.RequiredShotsPerInstance[tokens.Count]
				: info.RequiredShotsPerInstance[info.RequiredShotsPerInstance.Length - 1];

			if (shotsFired >= requiredShots)
			{
				if (info.IsCyclic && tokens.Count == info.MaximumInstances)
					RevokeInstance(self, true);
				else
					GrantInstance(self, info.Condition);

				shotsFired = 0;
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }
	}
}
