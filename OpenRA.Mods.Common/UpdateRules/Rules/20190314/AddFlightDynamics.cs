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
	public class AddFlightDynamics : UpdateRule
	{
		public override string Name { get { return "Replaced Aircraft boolean fields with FlightDynamics"; } }
		public override string Description
		{
			get
			{
				return "Various Aircraft boolean fields have been replaced with a single FlightDynamics flag list.\n"
					+ "Additionally, the old 'CanHover' behavior has been split into two flags:\n"
					+ "'Slide' to change flight direction independent of facing, and 'Hover' to be able to stand still mid-air.";
			}
		}

		static readonly string[] Properties =
		{
			"MoveIntoShroud",
			"CanHover",
			"VTOL",
			"TurnToLand",
			"TurnToDock",
			"TakeOffOnResupply",
			"TakeOffOnCreation",
		};

		readonly Dictionary<string, List<string>> locations = new Dictionary<string, List<string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "The following aircraft definitions did not use the internal defaults\n" +
					"and likely need a customized FlightDynamics flag list (and removal of the old booleans):\n" +
					UpdateUtils.FormatMessageList(locations.Select(
						kv => kv.Key + ":\n" + UpdateUtils.FormatMessageList(kv.Value)));

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraftTraits = actorNode.ChildrenMatching("Aircraft");
			foreach (var aircraft in aircraftTraits)
			{
				var used = new List<string>();
				foreach (var p in Properties)
					if (aircraft.LastChildMatching(p) != null)
						used.Add(p);

				if (used.Any())
				{
					var location = "{0} ({1})".F(actorNode.Key, actorNode.Location.Filename);
					locations[location] = used;
				}
			}

			yield break;
		}
	}
}
