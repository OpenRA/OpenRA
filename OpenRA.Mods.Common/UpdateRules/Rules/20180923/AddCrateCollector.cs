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
	public class AddCrateCollector : UpdateRule
	{
		public override string Name { get { return "Add CrateCollector"; } }
		public override string Description
		{
			get
			{
				return "Crate collection is now done via CrateCollector trait instead of crushing.";
			}
		}

		List<string> collectingLocomotors = new List<string>();
		List<MiniYamlNode> affectedActors = new List<MiniYamlNode>();

		bool addedCrateCollector;

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (!addedCrateCollector)
			{
				addedCrateCollector = true;
				foreach (var actor in affectedActors)
					AddCrateCollectorIfNecessary(actor);
			}

			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var crates = actorNode.ChildrenMatching("Crate");
			foreach (var crate in crates)
			{
				var crushClasses = crate.LastChildMatching("CrushClass");
				if (crushClasses != null)
					crushClasses.RenameKey("ValidCollectorTypes");

				var huf = actorNode.LastChildMatching("HiddenUnderFog");
				if (huf != null)
				{
					var type = huf.LastChildMatching("Type");
					if (type != null)
						type.Value.Value = "GroundPosition";
					else
						huf.AddNode("Type", "GroundPosition");
				}

				var hus = actorNode.LastChildMatching("HiddenUnderShroud");
				if (hus != null)
				{
					var type = hus.LastChildMatching("Type");
					if (type != null)
						type.Value.Value = "GroundPosition";
					else
						hus.AddNode("Type", "GroundPosition");
				}
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
				// If 'crate' is the only entry, remove Crushes node and add to list of crate crushing locos
				if (crushes.Value.Value == "crate")
				{
					collectingLocomotors.Add(name);
					locomotor.RemoveNode(crushes);
				}
				else
				{
					var oldCrushesEntries = FieldLoader.GetValue<string[]>("Crushes", crushes.Value.Value);
					if (oldCrushesEntries.Any(e => e == "crate"))
						collectingLocomotors.Add(name);

					var newCrushesEntries = oldCrushesEntries.Where(e => e != "crate");
					crushes.Value.Value = string.Join(", ", newCrushesEntries);
				}
			}
		}

		void AddCrateCollectorIfNecessary(MiniYamlNode actorNode)
		{
			// We only cached actors that have Mobile with a Locomotor: entry, so no null checks necessary here
			var mobile = actorNode.LastChildMatching("Mobile");
			var locomotorNode = mobile.LastChildMatching("Locomotor");

			// Just to be on the safe side, we check if the actor for some reason already has the CrateCollector trait
			var hasCrateCollector = actorNode.LastChildMatching("CrateCollector") != null;
			var crateCollectorNode = new MiniYamlNode("CrateCollector", "");
			if (!hasCrateCollector && collectingLocomotors.Any(l => l == locomotorNode.Value.Value))
				actorNode.AddNode(crateCollectorNode);
		}
	}
}
