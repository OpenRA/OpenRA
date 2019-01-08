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
	public class SplitGateFromBuilding : UpdateRule
	{
		public override string Name { get { return "Make gates use the 'Building' trait"; } }
		public override string Description
		{
			get
			{
				return "The 'Gate' trait does no longer inherit 'Building'.\n" +
					"Thus gates must define their own 'Building' trait.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var gate = actorNode.LastChildMatching("Gate");
			if (gate == null)
				yield break;

			var openSound = gate.LastChildMatching("OpeningSound");
			var closeSound = gate.LastChildMatching("ClosingSound");
			var closeDelay = gate.LastChildMatching("CloseDelay");
			var transitDelay = gate.LastChildMatching("TransitionDelay");
			var blockHeight = gate.LastChildMatching("BlocksProjectilesHeight");

			var newGate = new MiniYamlNode("Gate", "");
			gate.RenameKey("Building");

			if (openSound != null)
			{
				newGate.AddNode(openSound);
				gate.RemoveNode(openSound);
			}

			if (closeSound != null)
			{
				newGate.AddNode(closeSound);
				gate.RemoveNode(closeSound);
			}

			if (closeDelay != null)
			{
				newGate.AddNode(closeDelay);
				gate.RemoveNode(closeDelay);
			}

			if (transitDelay != null)
			{
				newGate.AddNode(transitDelay);
				gate.RemoveNode(transitDelay);
			}

			if (blockHeight != null)
			{
				newGate.AddNode(blockHeight);
				gate.RemoveNode(blockHeight);
			}

			actorNode.AddNode(newGate);
			yield break;
		}
	}
}
