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
	public class RenameChronoshiftFootprint : UpdateRule
	{
		public override string Name { get { return "Rename footprint related ChronoshiftPower parameters"; } }
		public override string Description
		{
			get
			{
				return "The parameters that define the footprint tiles to use in ChronoshiftPower\n" +
					"are renamed to follow standard conventions.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			// Repairable isn't conditional or otherwise supports multiple traits, so LastChildMatching should be fine.
			foreach (var placeBuilding in actorNode.ChildrenMatching("ChronoshiftPower"))
			{
				placeBuilding.RenameChildrenMatching("OverlaySpriteGroup", "FootprintImage");
				placeBuilding.RenameChildrenMatching("InvalidTileSequencePrefix", "InvalidFootprintSequence");
				placeBuilding.RenameChildrenMatching("SourceTileSequencePrefix", "SourceFootprintSequence");
				foreach (var valid in placeBuilding.ChildrenMatching("ValidTileSequencePrefix"))
				{
					valid.RenameKey("ValidFootprintSequence");
					valid.Value.Value = valid.Value.Value.Substring(0, valid.Value.Value.Length - 1);
				}
			}

			yield break;
		}
	}
}
