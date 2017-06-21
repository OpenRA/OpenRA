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

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/* Works without base engine modification */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	public class GrantConditionOnAttackInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		public object Create(ActorInitializer init) { return new GrantConditionOnAttack(init, this); }
	}

	public class GrantConditionOnAttack : INotifyCreated, ITick
	{
		readonly GrantConditionOnAttackInfo info;

		ConditionManager manager;
		int token = ConditionManager.InvalidConditionToken;

		public GrantConditionOnAttack(ActorInitializer init, GrantConditionOnAttackInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();
		}

		void GrantCondition(Actor self, string cond)
		{
			if (manager == null)
				return;

			if (string.IsNullOrEmpty(cond))
				return;

			token = manager.GrantCondition(self, cond);
		}

		void RevokeCondition(Actor self)
		{
			if (manager == null)
				return;

			if (token == ConditionManager.InvalidConditionToken)
				return;

			token = manager.RevokeCondition(self, token);
		}

		bool IsAttacking(Actor self)
		{
			// Currently only FlyAttack is supported, as I'm making this for Aurora.
			if (self.CurrentActivity is FlyAttack)
				return true;

			return false;
		}

		public void Tick(Actor self)
		{
			if (IsAttacking(self))
			{
				if (token == ConditionManager.InvalidConditionToken)
					GrantCondition(self, info.Condition);
			}
			else
			{
				if (token != ConditionManager.InvalidConditionToken)
					RevokeCondition(self);
			}
		}
	}
}