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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	sealed class RenameTypesUnderConsiderations : UpdateRule
	{
		public override string Name => "Rename Types to ValidTargetTypes under Considerations.";

		public override string Description => "Types -> ValidTargetTypes";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var spbm in actorNode.ChildrenMatching("SupportPowerBotModule"))
			{
				foreach (var decision in spbm.ChildrenMatching("Decisions"))
				{
					foreach (var sp in decision.Value.Nodes)
					{
						foreach (var consideration in sp.ChildrenMatching("Consideration"))
						{
							consideration.RenameChildrenMatching("Types", "ValidTargetTypes");
						}
					}
				}
			}

			yield break;
		}
	}
}
