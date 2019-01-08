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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemoveCanUndeployFromGrantConditionOnDeploy : UpdateRule
	{
		public override string Name { get { return "Remove CanUndeploy from GrantConditionOnDeploy."; } }
		public override string Description
		{
			get
			{
				return "The CanUndeploy property was removed from the GrantConditionOnDeploy trait.\n" +
					"Pausing the trait itself achieves the same effect now.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var gcod in actorNode.ChildrenMatching("GrantConditionOnDeploy"))
			{
				var canUndeploy = gcod.LastChildMatching("CanUndeploy");
				if (canUndeploy == null)
					continue;

				gcod.RemoveNode(canUndeploy);

				if (canUndeploy.NodeValue<bool>())
					continue;

				var deployedCondition = gcod.LastChildMatching("DeployedCondition");
				gcod.AddNode("PauseOnCondition", deployedCondition.Value.Value);
			}

			yield break;
		}
	}
}
