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

using System.Linq;
using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("General")]
	public class ConditionProperties : ScriptActorProperties, Requires<ExternalConditionInfo>
	{
		readonly ExternalCondition[] externalConditions;

		public ConditionProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			externalConditions = self.TraitsImplementing<ExternalCondition>().ToArray();
		}

		[Desc("Grant an external condition on this actor and return the revocation token.",
			"Conditions must be defined on an ExternalConditions trait on the actor.",
			"If duration > 0 the condition will be automatically revoked after the defined number of ticks.")]
		public int GrantCondition(string condition, int duration = 0)
		{
			var external = externalConditions
				.FirstOrDefault(t => t.Info.Condition == condition && t.CanGrantCondition(this));

			if (external == null)
				throw new LuaException($"Condition `{condition}` has not been listed on an enabled ExternalCondition trait");

			return external.GrantCondition(Self, this, duration);
		}

		[Desc("Revoke a condition using the token returned by GrantCondition.")]
		public void RevokeCondition(int token)
		{
			foreach (var external in externalConditions)
				if (external.TryRevokeCondition(Self, this, token))
					break;
		}

		[Desc("Check whether this actor accepts a specific external condition.")]
		public bool AcceptsCondition(string condition)
		{
			return externalConditions
				.Any(t => t.Info.Condition == condition && t.CanGrantCondition(this));
		}
	}
}
