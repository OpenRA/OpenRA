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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RenameSmudgeSmokeFields : UpdateRule
	{
		public override string Name => "Renamed smoke-related properties on SmudgeLayer.";

		public override string Description =>
			"Renamed smoke-related properties on SmudgeLayer to be in line with comparable properties.\n" +
			"Additionally, set the *Chance, *Image and *Sequences defaults to null.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var layer in actorNode.ChildrenMatching("SmudgeLayer"))
			{
				var chance = layer.LastChildMatching("SmokePercentage");
				if (chance != null)
					chance.RenameKey("SmokeChance");
				else
					layer.AddNode("SmokeChance", FieldSaver.FormatValue("25"));

				var image = layer.LastChildMatching("SmokeType");
				if (image != null)
					image.RenameKey("SmokeImage");
				else
					layer.AddNode("SmokeImage", FieldSaver.FormatValue("smoke_m"));

				var sequences = layer.LastChildMatching("SmokeSequence");
				if (sequences != null)
					sequences.RenameKey("SmokeSequences");
				else
					layer.AddNode("SmokeSequences", FieldSaver.FormatValue("idle"));
			}

			yield break;
		}
	}
}
