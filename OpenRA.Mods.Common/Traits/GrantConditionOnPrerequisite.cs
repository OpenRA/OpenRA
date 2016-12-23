#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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

	public class GrantConditionOnPrerequisite : INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly GrantConditionOnPrerequisiteInfo info;
		readonly GrantConditionOnPrerequisiteManager globalManager;

		ConditionManager manager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		bool wasAvailable;

		public GrantConditionOnPrerequisite(Actor self, GrantConditionOnPrerequisiteInfo info)
		{
			this.info = info;
			globalManager = self.Owner.PlayerActor.Trait<GrantConditionOnPrerequisiteManager>();
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.TraitOrDefault<ConditionManager>();
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

		public void PrerequisitesUpdated(Actor self, bool available)
		{
			if (available == wasAvailable || manager == null)
				return;

			if (available && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = manager.GrantCondition(self, info.Condition);
			else if (!available && conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = manager.RevokeCondition(self, conditionToken);

			wasAvailable = available;
		}
	}
}
