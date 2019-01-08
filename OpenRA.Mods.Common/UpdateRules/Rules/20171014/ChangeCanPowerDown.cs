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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ChangeCanPowerDown : UpdateRule
	{
		public override string Name { get { return "Provide a condition in 'CanPowerDown' instead of using 'Actor.Disabled'"; } }
		public override string Description
		{
			get
			{
				return "'CanPowerDown' now provides a condition instead of using the legacy 'Actor.Disabled' boolean.\n" +
					"Review your condition setup to make sure all relevant traits are disabled by that condition.";
			}
		}

		bool displayed;

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var canPowerDown = actorNode.LastChildMatching("CanPowerDown");
			if (canPowerDown == null)
				yield break;

			canPowerDown.AddNode("PowerdownCondition", "powerdown");

			var image = canPowerDown.LastChildMatching("IndicatorImage");
			var seq = canPowerDown.LastChildMatching("IndicatorSequence");
			var pal = canPowerDown.LastChildMatching("IndicatorPalette");
			var imageValue = image != null ? image.NodeValue<string>() : "poweroff";
			var seqValue = seq != null ? seq.NodeValue<string>() : "offline";
			var palValue = pal != null ? pal.NodeValue<string>() : "chrome";

			var indicator = new MiniYamlNode("WithDecoration@POWERDOWN", "");
			indicator.AddNode("Image", imageValue);
			indicator.AddNode("Sequence", seqValue);
			indicator.AddNode("Palette", palValue);
			indicator.AddNode("RequiresCondition", "powerdown");
			indicator.AddNode("ReferencePoint", "Center");

			actorNode.AddNode(indicator);
			if (image != null)
				canPowerDown.RemoveNodes("IndicatorImage");
			if (seq != null)
				canPowerDown.RemoveNodes("IndicatorSequence");
			if (pal != null)
				canPowerDown.RemoveNodes("IndicatorPalette");

			if (!displayed)
			{
				displayed = true;
				yield return "'CanPowerDown' now provides a condition. You might need to review your condition setup.";
			}
		}
	}
}
