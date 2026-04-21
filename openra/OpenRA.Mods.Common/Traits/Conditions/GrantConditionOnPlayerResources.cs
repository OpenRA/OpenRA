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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition to this actor when the player has stored resources.")]
	public class GrantConditionOnPlayerResourcesInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Enable condition when the amount of stored resources is greater than this.")]
		public readonly int Threshold = 0;

		public override object Create(ActorInitializer init) { return new GrantConditionOnPlayerResources(this); }
	}

	public class GrantConditionOnPlayerResources : INotifyCreated, INotifyOwnerChanged, ITick
	{
		readonly GrantConditionOnPlayerResourcesInfo info;
		PlayerResources playerResources;

		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnPlayerResources(GrantConditionOnPlayerResourcesInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		void ITick.Tick(Actor self)
		{
			if (string.IsNullOrEmpty(info.Condition))
				return;

			var enabled = playerResources.Resources > info.Threshold;
			if (enabled && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(info.Condition);
			else if (!enabled && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
