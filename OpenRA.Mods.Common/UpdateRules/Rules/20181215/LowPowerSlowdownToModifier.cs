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
	public class LowPowerSlowdownToModifier : UpdateRule
	{
		public override string Name { get { return "LowPowerSlowdown is renamed to LowPowerModifier"; } }
		public override string Description
		{
			get
			{
				return "ProductionQueue.LowPowerSlowdown is renamed to LowPowerModifier, and\n" +
					"multiplied by 100 to allow multiplying the build time with non-integer values.\n" +
					"Also default value is changed to 100.";
			}
		}

		readonly string[] queueTraits = { "ProductionQueue", "ClassicProductionQueue" };

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var queue in queueTraits.SelectMany(t => actorNode.ChildrenMatching(t)))
			{
				var lowPower = queue.LastChildMatching("LowPowerSlowdown");
				if (lowPower != null)
				{
					if (lowPower.NodeValue<int>() == 1)
						queue.RemoveNodes("LowPowerSlowdown");
					else
					{
						lowPower.RenameKey("LowPowerModifier");
						lowPower.ReplaceValue((lowPower.NodeValue<int>() * 100).ToString());
					}
				}
				else
					queue.AddNode("LowPowerModifier", "300");
			}

			yield break;
		}
	}
}
