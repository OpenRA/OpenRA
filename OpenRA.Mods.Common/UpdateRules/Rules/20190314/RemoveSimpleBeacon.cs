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
	public class RemoveSimpleBeacon : UpdateRule
	{
		public override string Name { get { return "Remove 'PlaceSimpleBeacon'."; } }
		public override string Description
		{
			get
			{
				return "The 'PlaceSimpleBeacon' trait was removed.\n" +
					"Use the new functionality of the 'PlaceBeacon' trait instead.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var psb = actorNode.LastChildMatching("PlaceSimpleBeacon");
			if (psb == null)
				yield break;

			psb.RenameKey("PlaceBeacon");

			var palette = psb.LastChildMatching("Palette");
			var isPlayer = psb.LastChildMatching("IsPlayerPalette");
			var sequence = psb.LastChildMatching("BeaconSequence");

			if (palette == null)
				psb.AddNode("Palette", "effect");

			if (isPlayer == null)
				psb.AddNode("IsPlayerPalette", "false");

			if (sequence == null)
				psb.AddNode("BeaconSequence", "idle");

			psb.AddNode("ArrowSequence", "");
			psb.AddNode("CircleSequence", "");

			yield break;
		}
	}
}
