#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AddActionsOnIdle : UpdateRule
	{
		public override string Name { get { return "Added ActionsOnIdle to Aircraft"; } }
		public override string Description
		{
			get
			{
				return "Added ActionsOnIdle property to Aircraft which replaces LandWhenIdle,\n" +
					"as well as the traits ReturnOnIdle and LeaveMapOnIdle.";
			}
		}

		bool messageShown;

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "The ReturnOnIdle and FlyAwayOnIdle traits as well as the LandWhenIdle property\n" +
				"and the hardcoded circling of AutoCarryall have been replaced with entries on ActionsOnIdle.\n" +
				"Check any actors that had one of these properties. Due to the internal default, other\n" +
				"aircraft should work as before without any changes.";

			if (!messageShown)
				yield return message;

			messageShown = true;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraft = actorNode.LastChildMatching("Aircraft");
			var autoCarryall = actorNode.LastChildMatching("AutoCarryall");
			var flyAwayOnIdle = actorNode.LastChildMatching("FlyAwayOnIdle");
			var returnOnIdle = actorNode.LastChildMatching("ReturnOnIdle");
			if (aircraft == null && autoCarryall == null && returnOnIdle == null && flyAwayOnIdle == null)
				yield break;

			var addActions = new List<string>();
			if (flyAwayOnIdle != null)
			{
				actorNode.RemoveNode(flyAwayOnIdle);
				addActions.Add("LeaveMap");
			}

			if (returnOnIdle != null)
			{
				actorNode.RemoveNode(returnOnIdle);
				addActions.Add("ReturnToBase");
			}

			if (autoCarryall != null)
				addActions.Add("Circle");

			if (aircraft != null)
			{
				// If addActions is still empty at this point, we want the default actions minus those where we know they're false
				if (addActions.Count == 0)
				{
					var landWhenIdle = aircraft.LastChildMatching("LandWhenIdle");
					if (landWhenIdle == null || landWhenIdle.NodeValue<bool>())
						addActions.Add("Land");

					var canHover = aircraft.LastChildMatching("CanHover");
					if (canHover == null || canHover.NodeValue<bool>())
						addActions.Add("Hover");

					addActions.Add("Circle");
					if (landWhenIdle != null)
						aircraft.RemoveNode(landWhenIdle);
				}

				var actionsOnIdle = new MiniYamlNode("ActionsOnIdle", addActions.JoinWith(", "));
				aircraft.AddNode(actionsOnIdle);
			}
			else
			{
				// If we got here, the actor has either one of the *OnIdle traits or AutoCarryall, but apparently
				// inherits Aircraft from elsewhere (or is an abstract actor that will be inherited instead),
				// so we need to add it ourselves.
				var addAircraft = new MiniYamlNode("Aircraft", "");
				var actionsOnIdle = new MiniYamlNode("ActionsOnIdle", addActions.JoinWith(", "));
				addAircraft.AddNode(actionsOnIdle);
				actorNode.AddNode(addAircraft);
			}

			yield break;
		}
	}
}
