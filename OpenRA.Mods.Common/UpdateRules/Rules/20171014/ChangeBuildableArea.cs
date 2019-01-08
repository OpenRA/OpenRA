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
	public class ChangeBuildableArea : UpdateRule
	{
		public override string Name { get { return "Require 'AreaTypes' on 'GivesBuildableArea'"; } }
		public override string Description
		{
			get
			{
				return "'AreaTypes' are now mandatory on 'GivesBuildableArea'.\n" +
				"A 'RequiresBuildableArea' trait was added and 'Building.Adjacent' was moved there.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var givesBuildableArea = actorNode.LastChildMatching("GivesBuildableArea");
			if (givesBuildableArea != null)
				givesBuildableArea.AddNode("AreaTypes", "building");

			var building = actorNode.LastChildMatching("Building");
			if (building != null)
			{
				var requiresBuildableArea = new MiniYamlNode("RequiresBuildableArea", "");
				requiresBuildableArea.AddNode("AreaTypes", "building");

				var adjacent = building.LastChildMatching("Adjacent");
				if (adjacent != null)
					requiresBuildableArea.AddNode(adjacent);

				actorNode.AddNode(requiresBuildableArea);
				building.RemoveNodes("Adjacent");
			}

			yield break;
		}
	}
}
