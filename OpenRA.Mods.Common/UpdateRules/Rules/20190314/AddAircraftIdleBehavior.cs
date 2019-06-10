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
	public class AddAircraftIdleBehavior : UpdateRule
	{
		public override string Name { get { return "Several aircraft traits and fields were replaced by Aircraft.IdleBehavior"; } }
		public override string Description
		{
			get
			{
				return "ReturnOnIdle and FlyAwayOnIdle traits as well as LandWhenIdle boolean\n"
					+ "were replaced by Aircraft.IdleBehavior.";
			}
		}

		readonly List<Tuple<string, string>> returnOnIdles = new List<Tuple<string, string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "ReturnOnIdle trait has been removed from the places listed below.\n"
				+ "Since this trait has been dysfunctional for a long time,\n"
				+ "IdleBehavior: ReturnToBase is NOT being set automatically.\n"
				+ "If you want your aircraft to return when idle, manually set it on the following definitions:\n"
				+ UpdateUtils.FormatMessageList(returnOnIdles.Select(n => n.Item1 + " (" + n.Item2 + ")"));

			if (returnOnIdles.Any())
				yield return message;

			returnOnIdles.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraft = actorNode.LastChildMatching("Aircraft");
			var returnOnIdle = actorNode.LastChildMatching("ReturnOnIdle");
			var flyAwayOnIdle = actorNode.LastChildMatching("FlyAwayOnIdle");

			if (aircraft != null)
			{
				var landWhenIdle = false;
				var landWhenIdleNode = aircraft.LastChildMatching("LandWhenIdle");
				if (landWhenIdleNode != null)
				{
					landWhenIdle = landWhenIdleNode.NodeValue<bool>();
					aircraft.RemoveNode(landWhenIdleNode);
				}

				// FlyAwayOnIdle should have had higher priority than LandWhenIdle even if both were 'true'.
				// ReturnOnIdle has been broken for so long that it's safer to ignore it here and only inform
				// the modder of the places it's been removed from, so they can change the IdleBehavior manually if desired.
				if (flyAwayOnIdle != null && !flyAwayOnIdle.IsRemoval())
					aircraft.AddNode(new MiniYamlNode("IdleBehavior", "LeaveMap"));
				else if (landWhenIdle)
					aircraft.AddNode(new MiniYamlNode("IdleBehavior", "Land"));
			}

			if (flyAwayOnIdle != null)
				actorNode.RemoveNode(flyAwayOnIdle);

			if (returnOnIdle != null)
			{
				returnOnIdles.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));
				actorNode.RemoveNode(returnOnIdle);
			}

			yield break;
		}
	}
}
