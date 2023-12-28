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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.AS.Scripting
{
	[ScriptPropertyGroup("General")]
	public class GrantConditionOnDeployProperties : ScriptActorProperties
	{
		readonly GrantConditionOnDeploy[] gcods;

		public GrantConditionOnDeployProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			gcods = self.TraitsImplementing<GrantConditionOnDeploy>().ToArray();
		}

		[ScriptActorPropertyActivity]
		[Desc("Deploy the actor.")]
		public void SwitchToDeploy()
		{
			foreach (var gcod in gcods)
				if (!gcod.IsTraitDisabled && !gcod.IsTraitPaused)
					Self.QueueActivity(new DeployForGrantedCondition(Self, gcod, DeployState.Deployed));
		}

		[ScriptActorPropertyActivity]
		[Desc("Undeploy the actor.")]
		public void SwitchToUndeploy()
		{
			foreach (var gcod in gcods)
				if (!gcod.IsTraitDisabled && !gcod.IsTraitPaused)
					Self.QueueActivity(new DeployForGrantedCondition(Self, gcod, DeployState.Undeployed));
		}
	}
}
