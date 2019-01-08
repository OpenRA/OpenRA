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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AircraftCanHoverGeneralization : UpdateRule
	{
		public override string Name { get { return "Split Aircraft.CanHover into multiple parameters"; } }
		public override string Description
		{
			get
			{
				return "Aircraft VTOL behaviour has been moved from CanHover to a new VTOL parameter.\n" +
					"Aircraft taking off automatically after reloading has been moved from CanHover to a new TakeOffOnResupply parameter.\n" +
					"Actors that set CanHover: true are updated with appropriate defaults for these parameters.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var aircraft in actorNode.ChildrenMatching("Aircraft"))
			{
				var canHover = aircraft.LastChildMatching("CanHover");
				if (canHover != null && canHover.NodeValue<bool>())
				{
					if (!aircraft.ChildrenMatching("TakeOffOnResupply").Any())
						aircraft.AddNode("TakeOffOnResupply", true);

					if (!aircraft.ChildrenMatching("VTOL").Any())
						aircraft.AddNode("VTOL", true);
				}
			}

			yield break;
		}
	}
}
