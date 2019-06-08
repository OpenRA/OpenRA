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
	public class AddAirAttackTypes : UpdateRule
	{
		public override string Name { get { return "Add AttackType field to AttackAircraft"; } }
		public override string Description
		{
			get
			{
				return "Aircraft attack behavior now depends on AttackAircraft.AttackType\n"
					+ "instead of Aircraft.CanHover.";
			}
		}

		readonly List<Tuple<string, string>> hoveringActors = new List<Tuple<string, string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "Aircraft attack behavior (Hover or Strafe) is now controlled via AttackAircraft.AttackType.\n"
				+ "Aircraft with CanHover: true will now also need AttackType: Hover on AttackAircraft\n"
				+ "to maintain position while attacking as before.\n"
				+ "The following places might need manual changes:\n"
				+ UpdateUtils.FormatMessageList(hoveringActors.Select(n => n.Item1 + " (" + n.Item2 + ")"));

			if (hoveringActors.Any())
				yield return message;

			hoveringActors.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraftTraits = actorNode.ChildrenMatching("Aircraft");
			var attackAircraftTraits = actorNode.ChildrenMatching("AttackAircraft");
			foreach (var attackAircraft in attackAircraftTraits)
			{
				var isHover = false;
				foreach (var aircraft in aircraftTraits)
				{
					var canHoverNode = aircraft.LastChildMatching("CanHover");
					if (canHoverNode != null)
						isHover = canHoverNode.NodeValue<bool>();

					if (isHover)
						break;
				}

				// It's still possible that CanHover: true is inherited, so let modders check manually if 'false',
				// otherwise add AttackType: Hover.
				if (!isHover)
					hoveringActors.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));
				else
					attackAircraft.AddNode("AttackType", "Hover");
			}

			yield break;
		}
	}
}
