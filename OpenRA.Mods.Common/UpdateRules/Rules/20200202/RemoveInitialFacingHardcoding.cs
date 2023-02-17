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
	public class RemoveInitialFacingHardcoding : UpdateRule
	{
		public override string Name => "Removed InitialFacing hardcoding for non-VTOLs";

		public override string Description => "Removed hardcoding of InitialFacing to 192 for aircraft with VTOL: false.";

		readonly List<Tuple<string, string>> nonVTOLs = new List<Tuple<string, string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "InitialFacing is no longer hardcoded to 192 for aircraft with VTOL: false.\n"
				+ "You may have to set it manually now in the following places:\n"
				+ UpdateUtils.FormatMessageList(nonVTOLs.Select(n => n.Item1 + " (" + n.Item2 + ")"));

			if (nonVTOLs.Count > 0)
				yield return message;

			nonVTOLs.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraft = actorNode.LastChildMatching("Aircraft");
			if (aircraft != null)
			{
				var initialFacing = aircraft.LastChildMatching("InitialFacing");

				var isVTOL = false;
				var vtolNode = aircraft.LastChildMatching("VTOL");
				if (vtolNode != null)
					isVTOL = vtolNode.NodeValue<bool>();

				// If InitialFacing is defined or it's a VTOL, no changes are needed.
				if (initialFacing != null || isVTOL)
					yield break;

				nonVTOLs.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));
			}
		}
	}
}
