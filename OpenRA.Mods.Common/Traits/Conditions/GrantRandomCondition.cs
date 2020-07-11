#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
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

		public override object Create(ActorInitializer init) { return new GrantRandomCondition(init.Self, this); }
	}

	public class GrantRandomCondition : INotifyCreated
	{
		readonly GrantRandomConditionInfo info;

		public GrantRandomCondition(Actor self, GrantRandomConditionInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (!info.Conditions.Any())
				return;

			var condition = info.Conditions.Random(self.World.SharedRandom);
			self.GrantCondition(condition);
		}
	}
}
