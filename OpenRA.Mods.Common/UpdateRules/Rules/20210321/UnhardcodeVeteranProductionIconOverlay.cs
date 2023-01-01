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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class UnhardcodeVeteranProductionIconOverlay : UpdateRule
	{
		public override string Name => "VeteranProductionIconOverlay is changed to ProductionIconOverlayManager giving it more customisation.";

		public override string Description => "ProductionIconOverlayManager now works with the new WithProductionIconOverlay trait, instead of ProducibleWithLevel.";

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "Icon overlay logic has been split from ProducibleWithLevel to WithProductionIconOverlay trait.\n" +
					"If you have been using VeteranProductionIconOverlay trait, add WithProductionIconOverlay to following actors:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var veteranProductionIconOverlay in actorNode.ChildrenMatching("VeteranProductionIconOverlay"))
			{
				veteranProductionIconOverlay.RenameKey("ProductionIconOverlayManager");
				veteranProductionIconOverlay.AddNode(new MiniYamlNode("Type", "Veterancy"));
			}

			foreach (var producibleWithLevel in actorNode.ChildrenMatching("ProducibleWithLevel"))
				locations.Add($"{actorNode.Key}: {producibleWithLevel.Key} ({actorNode.Location.Filename})");

			yield break;
		}
	}
}
