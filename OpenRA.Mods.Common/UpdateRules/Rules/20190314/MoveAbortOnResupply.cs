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
	public class MoveAbortOnResupply : UpdateRule
	{
		public override string Name { get { return "Moved AbortOnResupply from Aircraft to AttackAircraft"; } }
		public override string Description
		{
			get
			{
				return "AbortOnResupply boolean was moved to AttackAircraft.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraft = actorNode.LastChildMatching("Aircraft");
			var attackAircraft = actorNode.ChildrenMatching("AttackAircraft");

			if (aircraft != null)
			{
				var abortOnResupply = aircraft.LastChildMatching("AbortOnResupply");
				if (abortOnResupply == null)
					yield break;

				// Only add field to AttackAircraft if explicitly set to 'false'
				if (!abortOnResupply.NodeValue<bool>())
				{
					if (attackAircraft.Any())
						foreach (var a in attackAircraft)
							a.AddNode(abortOnResupply);
					else
					{
						var newAttackAircraft = new MiniYamlNode("AttackAircraft", "");
						newAttackAircraft.AddNode(abortOnResupply);
						actorNode.AddNode(newAttackAircraft);
					}
				}

				aircraft.RemoveNode(abortOnResupply);
			}

			yield break;
		}
	}
}
