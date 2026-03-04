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
	/// <summary>
	/// Remove MaximumDefenseRadius and replaces the MinimumDefenseRadius with TryMaintainDefenseRange
	/// with a new range check system.
	/// </summary>
	public class RemoveAndRenameDefenseRadiusInBaseBuilderBotModule : UpdateRule, IBeforeUpdateActors
	{
		public override string Name => "Remove and rename DefenseRadius in BaseBuilderBotModule";
		public override string Description => "Remove MaximumDefenseRadius and replaces the MinimumDefenseRadius with TryMaintainDefenseRange";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var baseBuilderBotModuleinfo in actorNode.ChildrenMatching("BaseBuilderBotModule"))
			{
				baseBuilderBotModuleinfo.RemoveNodes("MaximumDefenseRadius");
				baseBuilderBotModuleinfo.RenameChildrenMatching("MinimumDefenseRadius", "TryMaintainDefenseRange");
			}

			yield break;
		}
	}
}
