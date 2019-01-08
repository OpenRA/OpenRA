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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class DefineLocomotors : UpdateRule
	{
		public override string Name { get { return "Introduce global Locomotor definitions"; } }
		public override string Description
		{
			get
			{
				return "A large number of properties have been moved from the actor-level Mobile trait\n" +
					"to either world-level Locomotor traits or new actor-level GrantCondition* traits.\n" +
					"Conditions for subterranean and jumpjet behaviours are migrated,\n" +
					"and affected Mobile traits are listed for inspection.";
			}
		}

		readonly List<Tuple<string, string>> locations = new List<Tuple<string, string>>();
		bool subterraneanUsed;
		bool jumpjetUsed;

		readonly string[] locomotorFields =
		{
			"TerrainSpeeds", "Crushes", "CrushDamageTypes", "SharesCell", "MoveIntoShroud"
		};

		readonly string[] subterraneanFields =
		{
			"SubterraneanTransitionCost", "SubterraneanTransitionTerrainTypes",
			"SubterraneanTransitionOnRamps", "SubterraneanTransitionDepth",
			"SubterraneanTransitionPalette", "SubterraneanTransitionSound",
		};

		readonly string[] jumpjetFields =
		{
			"JumpjetTransitionCost", "JumpjetTransitionTerrainTypes", "JumpjetTransitionOnRamps"
		};

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "You must define a set of Locomotor traits on the World actor for the different\n"
				+ "movement classes used in your mod (e.g. Infantry, Vehicles, Tanks, Ships, etc)\n"
				+ "and replace any definitions/overrides of the following properties on each\n"
				+ "actor with a Locomotor field referencing the appropriate locomotor type.\n\n"
				+ "The standard Locomotor definition contains the following fields:\n"
				+ UpdateUtils.FormatMessageList(locomotorFields) + "\n\n";

			if (subterraneanUsed)
				message += "Actors using the subterranean logic should reference a SubterraneanLocomotor\n"
				+ "instance that extends Locomotor with additional fields:\n"
				+ UpdateUtils.FormatMessageList(subterraneanFields) + "\n\n";

			if (jumpjetUsed)
				message += "Actors using the jump-jet logic should reference a JumpjetLocomotor\n"
				+ "instance that extends Locomotor with additional fields:\n"
				+ UpdateUtils.FormatMessageList(jumpjetFields) + "\n\n";

			message += "Condition definitions have been automatically migrated.\n"
				+ "The following definitions reference fields that must be manually moved to Locomotors:\n"
				+ UpdateUtils.FormatMessageList(locations.Select(n => n.Item1 + " (" + n.Item2 + ")"));

			if (locations.Any())
				yield return message;

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var addNodes = new List<MiniYamlNode>();
			foreach (var mobileNode in actorNode.ChildrenMatching("Mobile"))
			{
				var checkFields = locomotorFields.Append(subterraneanFields).Append(jumpjetFields);
				if (checkFields.Any(f => mobileNode.ChildrenMatching(f).Any()))
					locations.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));

				var tunnelConditionNode = mobileNode.LastChildMatching("TunnelCondition");
				if (tunnelConditionNode != null)
				{
					var grantNode = new MiniYamlNode("GrantConditionOnTunnelLayer", "");
					tunnelConditionNode.MoveAndRenameNode(mobileNode, grantNode, "Condition");
					addNodes.Add(grantNode);
				}

				var subterraneanNode = mobileNode.LastChildMatching("Subterranean");
				if (subterraneanNode != null)
				{
					subterraneanUsed = true;

					mobileNode.RemoveNodes("Subterranean");
					var conditionNode = mobileNode.LastChildMatching("SubterraneanCondition");
					if (conditionNode != null)
						conditionNode.RenameKey("Condition");

					var transitionImageNode = mobileNode.LastChildMatching("SubterraneanTransitionImage");
					var transitionSequenceNode = mobileNode.LastChildMatching("SubterraneanTransitionSequence");
					var transitionPaletteNode = mobileNode.LastChildMatching("SubterraneanTransitionPalette");
					var transitionSoundNode = mobileNode.LastChildMatching("SubterraneanTransitionSound");

					var nodes = new[]
					{
						conditionNode,
						transitionImageNode,
						transitionSequenceNode,
						transitionPaletteNode,
						transitionSoundNode
					};

					if (nodes.Any(n => n != null))
					{
						var grantNode = new MiniYamlNode("GrantConditionOnSubterraneanLayer", "");
						foreach (var node in nodes)
							if (node != null)
								node.MoveNode(mobileNode, grantNode);

						addNodes.Add(grantNode);
					}
				}

				var jumpjetNode = mobileNode.LastChildMatching("Jumpjet");
				if (jumpjetNode != null)
				{
					jumpjetUsed = true;

					mobileNode.RemoveNodes("Jumpjet");
					var conditionNode = mobileNode.LastChildMatching("JumpjetCondition");
					if (conditionNode != null)
					{
						var grantNode = new MiniYamlNode("GrantConditionOnJumpjetLayer", "");
						conditionNode.MoveAndRenameNode(mobileNode, grantNode, "Condition");
						addNodes.Add(grantNode);
					}
				}
			}

			foreach (var node in addNodes)
				actorNode.AddNode(node);

			yield break;
		}
	}
}
