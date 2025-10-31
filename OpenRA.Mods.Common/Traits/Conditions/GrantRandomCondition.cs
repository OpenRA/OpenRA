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

namespace OpenRA.Mods.Common.Traits.Conditions
{
	[Desc("Grants a random condition from a predefined list to the actor when created.")]
	public class GrantRandomConditionInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("List of conditions to grant from.")]
		public readonly string[] Conditions = null;

		public override object Create(ActorInitializer init) { return new GrantRandomCondition(this); }
	}

	public class GrantRandomCondition : INotifyCreated
	{
		readonly GrantRandomConditionInfo info;

		public GrantRandomCondition(GrantRandomConditionInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (info.Conditions.Length == 0)
				return;

			var condition = info.Conditions.Random(self.World.SharedRandom);
			self.GrantCondition(condition);
		}
	}
}
