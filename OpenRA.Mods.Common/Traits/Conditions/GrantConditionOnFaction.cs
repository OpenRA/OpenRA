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
	[Desc("Grants a condition while the trait is active.")]
	class GrantConditionOnFactionInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Only grant this condition for certain factions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		[Desc("Should it recheck everything when it is captured?")]
		public readonly bool ResetOnOwnerChange = false;

		public override object Create(ActorInitializer init) { return new GrantConditionOnFaction(init, this); }
	}

	class GrantConditionOnFaction : ConditionalTrait<GrantConditionOnFactionInfo>, INotifyOwnerChanged
	{
		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;
		string faction;

		public GrantConditionOnFaction(ActorInitializer init, GrantConditionOnFactionInfo info)
			: base(info)
		{
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();

			base.Created(self);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (Info.ResetOnOwnerChange && faction != newOwner.Faction.InternalName)
			{
				faction = newOwner.Faction.InternalName;

				TraitDisabled(self);
				TraitEnabled(self);
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			if (conditionToken == ConditionManager.InvalidConditionToken && Info.Factions.Contains(faction))
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionToken == ConditionManager.InvalidConditionToken)
				return;

			conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}
	}
}
