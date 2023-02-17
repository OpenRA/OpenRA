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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemoveTurnToDock : UpdateRule
	{
		public override string Name => "TurnToDock is removed from the Aircraft trait.";

		public override string Description =>
			"TurnToDock is removed from the Aircraft trait in favor of letting the Exit trait on the host" +
			"building determine whether or not turning is required and to what facing the aircraft must turn.";

		readonly List<Tuple<string, string>> turningAircraft = new List<Tuple<string, string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "TurnToDock is now deprecated. The following actors had TurnToDock enabled:\n"
				+ UpdateUtils.FormatMessageList(turningAircraft.Select(n => n.Item1 + " (" + n.Item2 + ")"))
				+ "\n If you wish these units to keep their turning behaviour when docking with a host building" +
					" you will need to define a 'Facing' parameter on the 'Exit' trait of the host building. This change" +
					" does not affect the behaviour for landing on terrain which is governed by TurnToLand.";

			if (turningAircraft.Count > 0)
				yield return message;

			turningAircraft.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraft = actorNode.LastChildMatching("Aircraft");
			if (aircraft != null)
			{
				var turnToDock = aircraft.LastChildMatching("TurnToDock");
				if (turnToDock == null || !turnToDock.NodeValue<bool>())
					yield break;

				turningAircraft.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));
			}
		}
	}
}
