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
	public class ReplaceCanPowerDown : UpdateRule
	{
		public override string Name { get { return "Replace 'CanPowerDown' by 'ToggleConditionOnOrder'"; } }
		public override string Description
		{
			get
			{
				return "'CanPowerDown' was replaced with a more general 'ToggleConditionOnOrder' trait.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var cpd = actorNode.LastChildMatching("CanPowerDown");
			if (cpd == null)
				yield break;

			cpd.RenameKey("ToggleConditionOnOrder");
			var upSound = cpd.LastChildMatching("PowerupSound");
			if (upSound != null)
				upSound.RenameKey("DisabledSound");

			var upSpeech = cpd.LastChildMatching("PowerupSpeech");
			if (upSpeech != null)
				upSpeech.RenameKey("DisabledSpeech");

			var downSound = cpd.LastChildMatching("PowerdownSound");
			if (downSound != null)
				downSound.RenameKey("EnabledSound");

			var downSpeech = cpd.LastChildMatching("PowerdownSpeech");
			if (downSpeech != null)
				downSpeech.RenameKey("EnabledSpeech");

			cpd.AddNode("OrderName", "PowerDown");

			var condition = cpd.LastChildMatching("PowerdownCondition");
			if (condition != null)
				condition.RenameKey("Condition");
			else
				cpd.AddNode("Condition", "powerdown");

			if (cpd.ChildrenMatching("CancelWhenDisabled").Any())
			{
				cpd.RemoveNodes("CancelWhenDisabled");
				yield return "CancelWhenDisabled was removed when CanPowerDown was replaced by ToggleConditionOnOrder.\n" +
					"Use PauseOnCondition instead of RequiresCondition to replicate the behavior of 'false'.";
			}

			actorNode.AddNode(new MiniYamlNode("PowerMultiplier@POWERDOWN", new MiniYaml("", new List<MiniYamlNode>()
			{
				new MiniYamlNode("RequiresCondition", condition.Value.Value),
				new MiniYamlNode("Modifier", "0")
			})));
			yield break;
		}
	}
}
