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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition to this actor when the player has stored resources.")]
	public class GrantConditionOnPlayerResourcesInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Enable condition when the amount of stored resources is greater than this.")]
		public readonly int Threshold = 0;

		public object Create(ActorInitializer init) { return new GrantConditionOnPlayerResources(this); }
	}

	public class GrantConditionOnPlayerResources : INotifyCreated, INotifyOwnerChanged, ITick
	{
		readonly GrantConditionOnPlayerResourcesInfo info;
		PlayerResources playerResources;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnPlayerResources(GrantConditionOnPlayerResourcesInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;
			playerResources = playerActor.Trait<PlayerResources>();
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		void ITick.Tick(Actor self)
		{
			if (string.IsNullOrEmpty(info.Condition) || conditionManager == null)
				return;

			var enabled = playerResources.Resources > info.Threshold;
			if (enabled && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, info.Condition);
			else if (!enabled && conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}
	}
}
