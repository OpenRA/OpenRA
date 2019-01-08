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
	public class RequireProductionType : UpdateRule
	{
		public override string Name { get { return "Require 'ProductionType' on 'ProductionBar'"; } }
		public override string Description
		{
			get
			{
				return "The 'ProductionBar' trait now requires the 'ProductionType' to be set.\n" +
				"The value will be automatically set to the first value in 'Produces' of the first 'Production' trait.";
			}
		}

		readonly string[] productionTraits = { "Production", "ProductionAirdrop", "ProductionParadrop", "ProductionFromMapEdge" };

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var pb in actorNode.ChildrenMatching("ProductionBar"))
			{
				var type = pb.LastChildMatching("ProductionType");
				if (type != null)
					continue;

				MiniYamlNode production = null;
				foreach (var trait in productionTraits)
				{
					if (production != null)
						break;

					production = actorNode.ChildrenMatching(trait).FirstOrDefault();
				}

				if (production == null)
					continue;

				var produces = production.LastChildMatching("Produces");
				if (produces == null)
					continue;

				var toAdd = produces.NodeValue<string[]>().FirstOrDefault();
				if (toAdd != null)
					pb.AddNode("ProductionType", toAdd);
			}

			yield break;
		}
	}
}
