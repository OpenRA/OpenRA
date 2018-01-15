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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition to the actor this is attached to when prerequisites are available.")]
	public class GrantConditionOnPrerequisiteInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("List of required prerequisites.")]
		public readonly string[] Prerequisites = { };

		public object Create(ActorInitializer init) { return new GrantConditionOnPrerequisite(init.Self, this); }
	}

	public class GrantConditionOnPrerequisite : INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOwnerChanged
	{
		readonly GrantConditionOnPrerequisiteInfo info;

		bool wasAvailable;
		ConditionManager conditionManager;
		GrantConditionOnPrerequisiteManager globalManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnPrerequisite(Actor self, GrantConditionOnPrerequisiteInfo info)
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

			globalManager = playerActor.Trait<GrantConditionOnPrerequisiteManager>();
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (info.Prerequisites.Any())
				globalManager.Register(self, this, info.Prerequisites);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (info.Prerequisites.Any())
				globalManager.Unregister(self, this, info.Prerequisites);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			globalManager = newOwner.PlayerActor.Trait<GrantConditionOnPrerequisiteManager>();
		}

		public void PrerequisitesUpdated(Actor self, bool available)
		{
			if (available == wasAvailable || conditionManager == null)
				return;

			if (available && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, info.Condition);
			else if (!available && conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);

			wasAvailable = available;
		}
	}
}
