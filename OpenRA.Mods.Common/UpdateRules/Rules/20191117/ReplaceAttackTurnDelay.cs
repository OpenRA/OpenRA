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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ReplaceAttackTurnDelay : UpdateRule
	{
		public override string Name { get { return "Removed AttackTurnDelay from AttackAircraft and replaced with StrafeRunLength."; } }
		public override string Description
		{
			get
			{
				return "Removed AttackTurnDelay which defines the strafing run in ticks from AttackAircraft and\n"
				+ "replaced it with StrafeRunLength which defines the strafing run in WDist distance.";
			}
		}

		readonly List<Tuple<string, string>> strafers = new List<Tuple<string, string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "AttackTurnDelay is removed from AttackAircraft. Attack run lengths are now calculated dynamically.\n"
				+ "You may want to manually override the lengths in the following places by defining StrafeRunLength:\n"
				+ UpdateUtils.FormatMessageList(strafers.Select(n => n.Item1 + " (" + n.Item2 + ")"));

			if (strafers.Any())
				yield return message;

			strafers.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraft = actorNode.LastChildMatching("Aircraft");
			if (aircraft != null)
			{
				var attackAircraft = actorNode.LastChildMatching("AttackAircraft");
				if (attackAircraft == null)
					yield break;

				var canHover = aircraft.LastChildMatching("CanHover");
				var attackType = attackAircraft.LastChildMatching("AttackType");

				if (canHover != null && canHover.NodeValue<bool>() == true && attackType != null && attackType.NodeValue<AirAttackType>() != AirAttackType.Strafe)
					yield break;

				attackAircraft.RemoveNodes("AttackTurnDelay");
				strafers.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));
			}

			yield break;
		}
	}
}
