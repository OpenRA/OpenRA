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
		int conditionToken = Actor.InvalidConditionToken;
		string faction;

		public GrantConditionOnFaction(ActorInitializer init, GrantConditionOnFactionInfo info)
			: base(info)
		{
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
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
			if (conditionToken == Actor.InvalidConditionToken && Info.Factions.Contains(faction))
				conditionToken = self.GrantCondition(Info.Condition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
