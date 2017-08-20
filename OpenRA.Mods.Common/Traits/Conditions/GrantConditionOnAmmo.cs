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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Applies a condition based on the status of an AmmoPool.")]
	public class GrantConditionOnAmmoInfo : ITraitInfo, Requires<AmmoPoolInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Expression to evaluate. Use the variables \"amount\" for the current ammo count and \"capacity\" for the capacity.")]
		public readonly BooleanExpression Expression;

		[Desc("Name of the AmmoPools.")]
		public readonly string AmmoPoolName;

		public object Create(ActorInitializer init) { return new GrantConditionOnAmmo(init.Self, this); }
	}

	public class GrantConditionOnAmmo : INotifyCreated, ITick
	{
		readonly GrantConditionOnAmmoInfo info;
		readonly Dictionary<string, int> variables;
		readonly IReadOnlyDictionary<string, int> variablesReadOnly;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;
		AmmoPool[] ammoPools;

		public GrantConditionOnAmmo(Actor self, GrantConditionOnAmmoInfo info)
		{
			this.info = info;

			// TODO: Update these to account for disabled traits once AmmoPool is made conditional.
			variables = new Dictionary<string, int>()
			{
				{ "amount", 0 },
				{ "capacity", 0 }
			};
			variablesReadOnly = ReadOnlyDictionary.AsReadOnly(variables);
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			ammoPools = self.TraitsImplementing<AmmoPool>().Where(x => x.Info.Name == info.AmmoPoolName).ToArray();
		}

		public void Tick(Actor self)
		{
			variables["amount"] = ammoPools.Sum(x => x.GetAmmoCount());
			variables["capacity"] = ammoPools.Sum(x => x.Info.Ammo);

			var enabled = info.Expression.Evaluate(variablesReadOnly);
			if ((conditionToken != ConditionManager.InvalidConditionToken) != enabled)
			{
				if (enabled)
					conditionToken = conditionManager.GrantCondition(self, info.Condition);
				else
					conditionToken = conditionManager.RevokeCondition(self, conditionToken);
			}
		}
	}
}
