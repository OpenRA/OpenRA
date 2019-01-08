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
	public class ReplaceRequiresPower : UpdateRule
	{
		public override string Name { get { return "Replace 'RequiresPower' with 'GrantConditionOnPowerState'"; } }
		public override string Description
		{
			get
			{
				return "'RequiresPower' has been replaced with 'GrantConditionOnPowerState' which\n" +
					"toggles a condition depending on the power state.\nPossible PowerStates are: Normal " +
					"(0 or positive), Low (negative but higher than\n50% of required power) and Critical (below Low).";
			}
		}

		bool displayed;

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var requiresPower = actorNode.LastChildMatching("RequiresPower");
			if (requiresPower == null)
				yield break;

			requiresPower.RenameKey("GrantConditionOnPowerState@LOWPOWER", false);
			requiresPower.AddNode("Condition", "lowpower");
			requiresPower.AddNode("ValidPowerStates", "Low, Critical");

			if (!displayed)
			{
				displayed = true;
				yield return "'RequiresPower' was renamed to 'GrantConditionOnPowerState'.\n" +
					"You might need to review your condition setup.";
			}
		}
	}
}
