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
	public class RefactorHarvesterIdle : UpdateRule
	{
		public override string Name { get { return "Refactor harvester idle behavior."; } }
		public override string Description
		{
			get
			{
				return "The MaxIdleDuration parameter has been removed from the Harvester trait as part of a\n" +
					   " refactoring of harvester idling behavior.";
			}
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var t in actorNode.ChildrenMatching("Harvester"))
				if (t.RemoveNodes("MaxIdleDuration") > 0)
					locations.Add("{0} ({1})".F(actorNode.Key, actorNode.Location.Filename));

			yield break;
		}
	}
}
