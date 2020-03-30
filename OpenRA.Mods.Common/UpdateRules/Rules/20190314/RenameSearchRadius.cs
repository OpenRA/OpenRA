﻿#region Copyright & License Information
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
	class RenameSearchRadius : UpdateRule
	{
		public override string Name { get { return "Rename SearchFromOrderRadius to SearchFromHarvesterRadius"; } }
		public override string Description
		{
			get
			{
				return "Renamed 'SearchFromOrderRadius' to 'SearchFromHarvesterRadius'.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var harvester in actorNode.ChildrenMatching("Harvester"))
				harvester.RenameChildrenMatching("SearchFromOrderRadius", "SearchFromHarvesterRadius");

			yield break;
		}
	}
}
