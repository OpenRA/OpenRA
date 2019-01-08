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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class CapturableChanges : UpdateRule
	{
		public override string Name { get { return "Changes on 'Captures' and 'ExternalCaptures'"; } }
		public override string Description
		{
			get
			{
				return "'Type' was renamed to 'Types'. 'AllowAllies',\n" +
					"'AllowNeutral' and 'AllowEnemies' were replaced by 'ValidStances'.";
			}
		}

		void ApplyChanges(MiniYamlNode actorNode, MiniYamlNode node)
		{
			// Type renamed to Types
			var type = node.LastChildMatching("Type");
			if (type != null)
				type.RenameKey("Types");

			// Allow(Allies|Neutral|Enemies) replaced with a ValidStances enum
			var stance = Stance.Neutral | Stance.Enemy;
			var allowAllies = node.LastChildMatching("AllowAllies");
			if (allowAllies != null)
			{
				if (allowAllies.NodeValue<bool>())
					stance |= Stance.Ally;
				else
					stance &= ~Stance.Ally;

				node.RemoveNode(allowAllies);
			}

			var allowNeutral = node.LastChildMatching("AllowNeutral");
			if (allowNeutral != null)
			{
				if (allowNeutral.NodeValue<bool>())
					stance |= Stance.Neutral;
				else
					stance &= ~Stance.Neutral;

				node.RemoveNode(allowNeutral);
			}

			var allowEnemies = node.LastChildMatching("AllowEnemies");
			if (allowEnemies != null)
			{
				if (allowEnemies.NodeValue<bool>())
					stance |= Stance.Enemy;
				else
					stance &= ~Stance.Enemy;

				node.RemoveNode(allowEnemies);
			}

			if (stance != (Stance.Neutral | Stance.Enemy))
				node.AddNode("ValidStances", stance);
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var ca in actorNode.ChildrenMatching("Capturable"))
				ApplyChanges(actorNode, ca);

			foreach (var eca in actorNode.ChildrenMatching("ExternalCapturable"))
				ApplyChanges(actorNode, eca);

			yield break;
		}
	}
}
