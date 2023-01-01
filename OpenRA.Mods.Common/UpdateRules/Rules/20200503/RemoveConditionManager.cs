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
	public class RemoveConditionManager : UpdateRule
	{
		public override string Name => "ConditionManager trait has been removed.";

		public override string Description => "ConditionManager trait has been removed. Its functionality has been integrated into the actor itself.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			actorNode.RemoveNodes("ConditionManager");
			yield break;
		}
	}
}
