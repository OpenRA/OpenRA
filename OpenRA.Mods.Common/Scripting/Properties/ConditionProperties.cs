#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("General")]
	public class ConditionProperties : ScriptActorProperties, Requires<ConditionManagerInfo>
	{
		ConditionManager um;
		public ConditionProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			um = self.Trait<ConditionManager>();
		}

		[Desc("Grant a condition level to this actor.")]
		public void GrantConditionLevel(string condition)
		{
			um.GrantCondition(self, condition, this);
		}

		[Desc("Revoke a condition level that was previously granted using GrantUpgrade.")]
		public void RevokeCondition(string condition)
		{
			um.RevokeCondition(self, condition, this);
		}

		[Desc("Grant a limited-time condition level to this actor.")]
		public void GrantTimedCondition(string condition, int duration)
		{
			um.GrantTimedCondition(self, condition, duration);
		}

		[Desc("Check whether this actor accepts a specific condition.")]
		public bool AcceptsConditionType(string condition)
		{
			return um.AcceptsConditionType(self, condition);
		}
	}
}