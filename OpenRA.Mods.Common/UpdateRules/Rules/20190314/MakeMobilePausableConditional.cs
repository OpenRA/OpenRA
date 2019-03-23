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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class MakeMobilePausableConditional : UpdateRule
	{
		public override string Name { get { return "Change Mobile>RequiresCondition to PauseOnCondition"; } }
		public override string Description
		{
			get
			{
				return "Mobile is now a PausableConditionalTrait instead of a ConditionalTrait.\n" +
                "RequiresCondition is changed to PauseOnCondition.";
			}
		}

		bool displayedMessage;
		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "You may want to update the result of PauseOnCondition, as this update\n" +
				"just adds ! prefix to RequiresCondition's value to reverse it.";

			if (!displayedMessage)
				yield return message;

			displayedMessage = true;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var node in actorNode.ChildrenMatching("Mobile").Where(t => t.ChildrenMatching("RequiresCondition").Any()))
			{
				var rc = node.LastChildMatching("RequiresCondition");

				rc.ReplaceValue("!(" + rc.Value.Value + ")");
				rc.RenameKey("PauseOnCondition");
			}

			yield break;
		}
	}
}
