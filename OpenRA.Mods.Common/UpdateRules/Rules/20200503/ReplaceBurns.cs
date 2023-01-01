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
	public class ReplaceBurns : UpdateRule
	{
		public override string Name => "Replaced Burns with separate render and health change traits.";

		public override string Description => "Burns can be replaced using WithIdleOverlay and ChangesHealth.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var addNodes = new List<MiniYamlNode>();

			foreach (var burns in actorNode.ChildrenMatching("Burns"))
			{
				var anim = burns.LastChildMatching("Anim");
				var animValue = anim != null ? anim.NodeValue<string>() : "1";

				var damage = burns.LastChildMatching("Damage");
				var damageValue = damage != null ? damage.NodeValue<int>() : 1;

				var interval = burns.LastChildMatching("Interval");
				var intervalValue = interval != null ? interval.NodeValue<int>() : 8;

				var overlay = new MiniYamlNode("WithIdleOverlay@Burns", "");
				overlay.AddNode("Image", FieldSaver.FormatValue("fire"));
				overlay.AddNode("Sequence", FieldSaver.FormatValue(animValue));
				overlay.AddNode("IsDecoration", FieldSaver.FormatValue(true));
				addNodes.Add(overlay);

				var changesHealth = new MiniYamlNode("ChangesHealth", "");
				changesHealth.AddNode("Step", FieldSaver.FormatValue(-damageValue));
				changesHealth.AddNode("StartIfBelow", FieldSaver.FormatValue(101));
				changesHealth.AddNode("Delay", FieldSaver.FormatValue(intervalValue));
				addNodes.Add(changesHealth);
			}

			actorNode.RemoveNodes("Burns");

			foreach (var addNode in addNodes)
				actorNode.AddNode(addNode);

			yield break;
		}
	}
}
