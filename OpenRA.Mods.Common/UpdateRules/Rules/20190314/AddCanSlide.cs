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
	public class AddCanSlide : UpdateRule
	{
		public override string Name { get { return "Split CanSlide from CanHover"; } }
		public override string Description
		{
			get
			{
				return "Aircraft.CanHover was split into two flags; CanHover now only makes aircraft hover when idle,\n"
					+ "while CanSlide toggles the ability to immediately changing direction without flying a curve.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraftTraits = actorNode.ChildrenMatching("Aircraft");
			foreach (var aircraft in aircraftTraits)
			{
				var canHover = false;
				var canHoverNode = aircraft.LastChildMatching("CanHover");
				if (canHoverNode != null)
					canHover = canHoverNode.NodeValue<bool>();
				else
					yield break;

				aircraft.AddNode("CanSlide", canHover.ToString());
			}

			yield break;
		}
	}
}
