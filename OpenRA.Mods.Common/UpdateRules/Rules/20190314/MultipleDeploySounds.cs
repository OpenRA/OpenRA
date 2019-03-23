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

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class MultipleDeploySounds : UpdateRule
	{
		public override string Name { get { return "'GrantConditionOnDeploy' now supports multiple (un)deploy sounds"; } }
		public override string Description
		{
			get
			{
				return "Renamed 'DeploySound' to 'DeploySounds' and 'UndeploySound' to 'UndeploySounds'\n" +
					"on 'GrantConditionOnDeploy'.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var grants = actorNode.ChildrenMatching("GrantConditionOnDeploy");
			foreach (var g in grants)
			{
				var deploy = g.LastChildMatching("DeploySound");
				if (deploy != null)
					deploy.RenameKey("DeploySounds");

				var undeploy = g.LastChildMatching("UndeploySound");
				if (undeploy != null)
					undeploy.RenameKey("UndeploySounds");
			}

			yield break;
		}
	}
}
