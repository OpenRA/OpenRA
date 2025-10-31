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
	public class GrantConditionOnLineBuildDirectionInfo : TraitInfo, Requires<LineBuildInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Line build direction to trigger the condition.")]
		public readonly LineBuildDirection Direction = LineBuildDirection.X;

		public override object Create(ActorInitializer init) { return new GrantConditionOnLineBuildDirection(init, this); }
	}

	public class GrantConditionOnLineBuildDirection : INotifyCreated
	{
		readonly GrantConditionOnLineBuildDirectionInfo info;
		readonly LineBuildDirection direction;

		public GrantConditionOnLineBuildDirection(ActorInitializer init, GrantConditionOnLineBuildDirectionInfo info)
		{
			this.info = info;
			direction = init.GetValue<LineBuildDirectionInit, LineBuildDirection>();
		}

		void INotifyCreated.Created(Actor self)
		{
			if (direction == info.Direction)
				self.GrantCondition(info.Condition);
		}
	}
}
