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
	public class RenameSearchFromProcRadius : UpdateRule
	{
		public override string Name { get { return "SearchFromProcRadius renamed to SearchFromRefineryRadius"; } }
		public override string Description
		{
			get
			{
				return "'Proc' is the RA abbreviation for [Ore] Processor.\n" +
					"'Refinery' is the established generic name for where harvesters dump their resources.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var harv in actorNode.ChildrenMatching("Harvester"))
				harv.RenameChildrenMatching("SearchFromProcRadius", "SearchFromRefineryRadius");

			yield break;
		}
	}
}
