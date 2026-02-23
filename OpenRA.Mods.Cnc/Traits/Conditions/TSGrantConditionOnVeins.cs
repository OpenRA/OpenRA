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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Grants a condition to this actor when the player is full on veins.")]
	public class TSGrantConditionOnVeinsInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new TSGrantConditionOnVeins(this); }
	}

	public class TSGrantConditionOnVeins : INotifyCreated, ITick
	{
		readonly TSGrantConditionOnVeinsInfo info;
		TSPlayerResources playerResources;

		int conditionToken = Actor.InvalidConditionToken;

		public TSGrantConditionOnVeins(TSGrantConditionOnVeinsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			playerResources = self.Owner.PlayerActor.Trait<TSPlayerResources>();
		}


		void ITick.Tick(Actor self)
		{
			if (string.IsNullOrEmpty(info.Condition))
				return;

			var enabled = playerResources.Veins > playerResources.Info.TriggerChemicalMissileOnVeinsAmount;
			if (enabled && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(info.Condition);
			else if (!enabled && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
