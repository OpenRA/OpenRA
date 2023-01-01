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
	public class RemoveMuzzleSplitFacings : UpdateRule
	{
		public override string Name => "Remove Armament.MuzzleSplitFacings.";

		public override string Description =>
			"The legacy MuzzleSplitFacings option was removed from Armament.\n" +
			"The same result can be created by using `Combine` in the sequence definitions to\n" +
			"assemble the different facings sprites into a single sequence.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var a in actorNode.ChildrenMatching("Armament"))
			{
				var muzzleSplitFacings = a.LastChildMatching("MuzzleSplitFacings");
				var sequenceNode = a.LastChildMatching("MuzzleSequence");
				if (muzzleSplitFacings != null && sequenceNode != null)
				{
					var sequence = sequenceNode.Value.Value;
					var facings = muzzleSplitFacings.NodeValue<int>() - 1;
					var actor = actorNode.Key.ToLowerInvariant();
					yield return
						$"The Armament muzzle effect has been removed from {actor} ({actorNode.Location.Filename}).\n" +
						$"If you would like to restore the muzzle effect you must redefine `MuzzleSequence: {sequence}`\n" +
						$"and replace the {sequence}0-{facings} sequence definitions with a single `{sequence}` sequence that uses\n" +
						"the Combine syntax to assemble the different facing sprites.";

					a.RemoveNode(muzzleSplitFacings);
					a.RemoveNode(sequenceNode);
				}
			}
		}
	}
}
