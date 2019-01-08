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
	public class MergeCaptureTraits : UpdateRule
	{
		public override string Name { get { return "Merge and overhaul Captures traits"; } }
		public override string Description
		{
			get
			{
				return "The internal and external capturing traits have been merged, and a new\n" +
					"CaptureManager trait has been added to help manage interactions between\n" +
					"actors and multiple trait instances. The Sabotage logic has also\n" +
					"moved from the Capturable trait to the Captures trait.\n" +
					"The External* traits are renamed, and the CaptureManager added wherever\n" +
					"the Capturable or Captures trait is used. The locations modified are\n" +
					"listed for inspection and manual cleanup";
			}
		}

		readonly List<string> captureManagerLocations = new List<string>();
		readonly List<string> externalDelayLocations = new List<string>();
		readonly List<string> sabotageLocations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (captureManagerLocations.Any())
				yield return "The Captures and Capturable traits now depend on the\n" +
					"new CaptureManager trait. This trait has automatically been added\n" +
					"in the following definitions:\n" + UpdateUtils.FormatMessageList(captureManagerLocations) +
					"\nYou may wish to review these definitions and delete any redundant\n" +
					"instances that have already been inherited from a parent template.";

			if (sabotageLocations.Any())
				yield return "The sabotage logic has been disabled by default, which affects\n" +
					"the following definitions:\n" + UpdateUtils.FormatMessageList(sabotageLocations) +
					"\nThe sabotage logic is now defined on the Captures trait, via the \n" +
					"SabotageThreshold field. You may need to define additional capture types\n" +
					"and Capturable traits if you wish to use multiple capture thresholds.";

			if (externalDelayLocations.Any())
				yield return "The following actors have had their capture delays reset to 15 seconds:\n" +
					UpdateUtils.FormatMessageList(sabotageLocations) +
					"\nThe capture delay is now defined on the Captures trait, via the\n" +
					"CaptureDelay field. You may need to define additional capture types\n" +
					"and Capturable traits if you wish to use multiple capture delays.";

			captureManagerLocations.Clear();
			sabotageLocations.Clear();
			externalDelayLocations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var reportLocation = "{0} ({1})".F(actorNode.Key, actorNode.Location.Filename);
			var captureManager = actorNode.LastChildMatching("CaptureManager") ?? new MiniYamlNode("CaptureManager", "");
			var usesCaptureManager = false;

			// Migrate External*
			foreach (var c in actorNode.ChildrenMatching("ExternalCapturable"))
			{
				c.RenameKey("Capturable");
				if (c.RemoveNodes("CaptureCompleteTime") > 0)
					externalDelayLocations.Add(reportLocation);
			}

			foreach (var c in actorNode.ChildrenMatching("ExternalCaptures"))
			{
				c.RenameKey("Captures");
				if (c.Key.StartsWith("-"))
					continue;

				c.AddNode("CaptureDelay", 375);

				var consumeNode = c.LastChildMatching("ConsumeActor");
				if (consumeNode != null)
					consumeNode.RenameKey("ConsumedByCapture");
				else
					c.AddNode("ConsumedByCapture", false);

				var conditionNode = c.LastChildMatching("CapturingCondition");
				if (conditionNode != null)
					conditionNode.MoveNode(c, captureManager);

				var cursorNode = c.LastChildMatching("CaptureCursor");
				if (cursorNode != null)
					cursorNode.RenameKey("EnterCursor");
				else
					c.AddNode("EnterCursor", "ability");

				var cursorBlockedNode = c.LastChildMatching("CaptureBlockedCursor");
				if (cursorBlockedNode != null)
					cursorBlockedNode.RenameKey("EnterBlockedCursor");
				else
					c.AddNode("EnterBlockedCursor", "move-blocked");
			}

			var addBlinker = false;
			foreach (var c in actorNode.ChildrenMatching("ExternalCapturableBar"))
			{
				c.RenameKey("CapturableProgressBar");
				addBlinker = true;
			}

			if (addBlinker)
				actorNode.AddNode("CapturableProgressBlink", "");

			// Remove any CaptureThreshold nodes and restore the "building" default
			// These run on converted External* traits too
			foreach (var traitNode in actorNode.ChildrenMatching("Capturable"))
			{
				if (!traitNode.Key.StartsWith("-"))
					usesCaptureManager = true;

				if (traitNode.RemoveNodes("CaptureThreshold") + traitNode.RemoveNodes("Sabotage") > 0)
					sabotageLocations.Add(reportLocation);

				if (!traitNode.Key.StartsWith("-") && traitNode.LastChildMatching("Types") == null)
					traitNode.AddNode("Types", "building");
			}

			foreach (var traitNode in actorNode.ChildrenMatching("Captures"))
			{
				if (!traitNode.Key.StartsWith("-"))
					usesCaptureManager = true;

				if (traitNode.LastChildMatching("CaptureTypes") == null)
					traitNode.AddNode("CaptureTypes", "building");
			}

			if (usesCaptureManager && actorNode.LastChildMatching("CaptureManager") == null)
			{
				actorNode.AddNode(captureManager);
				captureManagerLocations.Add(reportLocation);
			}

			yield break;
		}
	}
}
