#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class AddMineDetonator : UpdateRule
	{
		public override string Name { get { return "Add MineDetonator"; } }
		public override string Description
		{
			get
			{
				return "Mine detonation is now done via MineDetonator trait instead of crushing.";
			}
		}

		List<string> collectingLocomotors = new List<string>();
		List<MiniYamlNode> affectedActors = new List<MiniYamlNode>();

		bool addedMineDetonator;

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (!addedMineDetonator)
			{
				addedMineDetonator = true;
				foreach (var actor in affectedActors)
					AddMineDetonatorIfNecessary(actor);
			}

			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var mines = actorNode.ChildrenMatching("Mine");
			foreach (var mine in mines)
			{
				var crushClasses = mine.LastChildMatching("CrushClass");
				if (crushClasses != null)
					crushClasses.RenameKey("ValidDetonatorTypes");
			}

			var locomotors = actorNode.ChildrenMatching("Locomotor");
			foreach (var locomotor in locomotors)
				CheckAndUpdateLocomotor(locomotor);

			var jjLocomotors = actorNode.ChildrenMatching("JumpjetLocomotor");
			foreach (var locomotor in jjLocomotors)
				CheckAndUpdateLocomotor(locomotor);

			var stLocomotors = actorNode.ChildrenMatching("SubterraneanLocomotor");
			foreach (var locomotor in stLocomotors)
				CheckAndUpdateLocomotor(locomotor);

			// Caching actors with defined locomotor because we have to add CrateCollector in AfterUpdate,
			// since otherwise it would be impossible to ensure we've checked the locomotors for crate collect ability.
			var mobile = actorNode.LastChildMatching("Mobile");
			if (mobile != null)
			{
				var locomotor = mobile.LastChildMatching("Locomotor");
				if (locomotor != null)
					affectedActors.Add(actorNode);
			}

			yield break;
		}

		void CheckAndUpdateLocomotor(MiniYamlNode locomotor)
		{
			var name = "default";
			var nameNode = locomotor.LastChildMatching("Name");
			if (nameNode != null)
				name = nameNode.Value.Value;

			var crushes = locomotor.LastChildMatching("Crushes");
			if (crushes != null)
			{
				// If 'mine' is the only entry, remove Crushes node and add to list of crate crushing locos
				if (crushes.Value.Value == "mine")
				{
					collectingLocomotors.Add(name);
					locomotor.RemoveNode(crushes);
				}
				else
				{
					var oldCrushesEntries = FieldLoader.GetValue<string[]>("Crushes", crushes.Value.Value);
					if (oldCrushesEntries.Any(e => e == "mine"))
						collectingLocomotors.Add(name);

					var newCrushesEntries = oldCrushesEntries.Where(e => e != "mine");
					crushes.Value.Value = string.Join(", ", newCrushesEntries);
				}
			}
		}

		void AddMineDetonatorIfNecessary(MiniYamlNode actorNode)
		{
			// We only cached actors that have Mobile with a Locomotor: entry, so no null checks necessary here
			var mobile = actorNode.LastChildMatching("Mobile");
			var locomotorNode = mobile.LastChildMatching("Locomotor");

			// Just to be on the safe side, we check if the actor for some reason already has the CrateCollector trait
			var hasMineDetonator = actorNode.LastChildMatching("MineDetonator") != null;
			var crateMineDetonatorNode = new MiniYamlNode("MineDetonator", "");
			if (!hasMineDetonator && collectingLocomotors.Any(l => l == locomotorNode.Value.Value))
				actorNode.AddNode(crateMineDetonatorNode);
		}
	}
}
