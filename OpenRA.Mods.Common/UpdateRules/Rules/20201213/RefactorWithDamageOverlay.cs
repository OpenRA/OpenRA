#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
	public class RefactorWithDamageOverlay : UpdateRule
	{
		public override string Name => "Refactored WithDamageOverlay to be more configurable.";

		public override string Description =>
			"Internal defaults for Image and all 3 *Sequence properties were removed.\n" +
			"IdleSequence was renamed to StartSequence and made optional, EndSequence was also made optional.\n" +
			"LoopSequence can now actually be looped via the new LoopCount property:\n" +
			"Either a fixed number of times, or (by setting a negative value) until hitting an invalid damage state.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var damageOverlay in actorNode.ChildrenMatching("WithDamageOverlay"))
			{
				var imageNode = damageOverlay.LastChildMatching("Image");

				var idleSequenceNode = damageOverlay.LastChildMatching("IdleSequence");
				var loopSequenceNode = damageOverlay.LastChildMatching("LoopSequence");
				var endSequenceNode = damageOverlay.LastChildMatching("EndSequence");

				if (imageNode == null)
					damageOverlay.AddNode("Image", FieldSaver.FormatValue("smoke_m"));

				if (idleSequenceNode == null)
					damageOverlay.AddNode("StartSequence", FieldSaver.FormatValue("idle"));
				else
					idleSequenceNode.RenameKey("StartSequence");

				if (loopSequenceNode == null)
					damageOverlay.AddNode("LoopSequence", FieldSaver.FormatValue("loop"));

				if (endSequenceNode == null)
					damageOverlay.AddNode("EndSequence", FieldSaver.FormatValue("end"));
			}

			yield break;
		}
	}
}
