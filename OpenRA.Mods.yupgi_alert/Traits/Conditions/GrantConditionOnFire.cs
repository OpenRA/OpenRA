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
	public class GrantConditionOnFireInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		[WeaponReference]
		[Desc("Name of the armament that grants this condition")]
		public readonly string Armament = null;

		[Desc("How many shots to accumulate condition?",
			"On gattling, since shots get faster, you can define multiple values at each level.",
			"If stack is higher than the values defined here, it will use the last value defined here.")]
		public readonly int[] RequiredShotsPerStack = { 3 };

		[Desc("Max Stacking of conditions?")]
		public readonly int MaxStacking = 2;

		[Desc("Without firing for this period of time, we unstack one level of condition.")]
		public readonly int UnstackDelay = 15;

		[Desc("Discard stack, if you switch target? You probably want PopAll too.")]
		public readonly bool UnstackOnNewTarget = false;

		[Desc("Pop all condition stack on unstack?")]
		public readonly bool PopAll = false;

		public object Create(ActorInitializer init) { return new GrantConditionOnFire(init, this); }
	}

	public class GrantConditionOnFire : INotifyCreated, ITick, INotifyAttack
	{
		readonly GrantConditionOnFireInfo info;
		readonly Stack<int> tokens = new Stack<int>();

		int cooldown = 0;
		int shotsFired = 0;
		ConditionManager manager;

		public GrantConditionOnFire(ActorInitializer init, GrantConditionOnFireInfo info)
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

		void UnstackCondition(Actor self)
		{
			shotsFired = 0;

			if (manager == null)
				return;

			if (tokens.Count == 0)
				return;

			if (!info.PopAll)
				manager.RevokeCondition(self, tokens.Pop());
			else
				while (tokens.Count > 0)
					manager.RevokeCondition(self, tokens.Pop());
		}

		public void Tick(Actor self)
		{
			if (cooldown <= 0)
				return;

			cooldown--;
			if (cooldown <= 0)
			{
				cooldown = info.UnstackDelay;
				UnstackCondition(self);
			}
		}

		bool TargetChanged(Target lastTarget, Target target)
		{
			if (lastTarget.Type == TargetType.Invalid)
				return true;

			if (target.Type == TargetType.Invalid)
				return true;

			// Same actor, fine.
			if (lastTarget.Actor == target.Actor)
				return false;

			// Position designated. No position change = same target.
			if (lastTarget.CenterPosition == target.CenterPosition)
				return false;

			return true;
		}

		Target lastTarget;
		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (a.Info.Name != info.Armament)
				return;

			if (info.UnstackOnNewTarget)
			{
				if (TargetChanged(lastTarget, target))
					UnstackCondition(self);
				lastTarget = target;
			}

			cooldown = info.UnstackDelay;

			if (tokens.Count >= info.MaxStacking)
				return;

			shotsFired++;
			var requiredShots = tokens.Count < info.RequiredShotsPerStack.Length ?
				info.RequiredShotsPerStack[tokens.Count] :
				info.RequiredShotsPerStack[info.RequiredShotsPerStack.Length - 1];
			if (shotsFired >= requiredShots)
			{
				StackCondition(self, info.Condition);
				shotsFired = 0;
			}
		}

		public void PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{
			// do nothing
		}
	}
}