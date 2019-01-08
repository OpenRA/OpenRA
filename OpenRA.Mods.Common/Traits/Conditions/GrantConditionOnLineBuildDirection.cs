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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class GrantConditionOnLineBuildDirectionInfo : ITraitInfo, Requires<LineBuildInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Line build direction to trigger the condition.")]
		public readonly LineBuildDirection Direction = LineBuildDirection.X;

		public object Create(ActorInitializer init) { return new GrantConditionOnLineBuildDirection(init, this); }
	}

	public class GrantConditionOnLineBuildDirection : INotifyCreated
	{
		readonly GrantConditionOnLineBuildDirectionInfo info;
		readonly LineBuildDirection direction;

		public GrantConditionOnLineBuildDirection(ActorInitializer init, GrantConditionOnLineBuildDirectionInfo info)
		{
			this.info = info;
			direction = init.Get<LineBuildDirectionInit>().Value(init.World);
		}

		void INotifyCreated.Created(Actor self)
		{
			if (direction != info.Direction)
				return;

			var conditionManager = self.TraitOrDefault<ConditionManager>();
			if (conditionManager != null && !string.IsNullOrEmpty(info.Condition))
				conditionManager.GrantCondition(self, info.Condition);
		}
	}
}
