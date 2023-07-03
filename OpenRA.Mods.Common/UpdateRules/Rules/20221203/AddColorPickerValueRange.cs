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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AddColorPickerValueRange : UpdateRule
	{
		public override string Name => "ColorPickerManager's PresetHues, PresetSaturations and V were replaced with PresetColors.";

		public override string Description =>
			"Each preset color can now have their brightness specified. SimilarityThreshold range was changed.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var manager = actorNode.LastChildMatching("ColorPickerManager");
			if (manager == null)
				yield break;

			manager.RemoveNodes("SimilarityThreshold");

			var v = manager.LastChildMatching("V")?.NodeValue<float>() ?? 0.95f;
			var hues = manager.LastChildMatching("PresetHues")?.NodeValue<float[]>();
			var saturations = manager.LastChildMatching("PresetSaturations")?.NodeValue<float[]>();

			manager.RemoveNodes("V");
			manager.RemoveNodes("PresetHues");
			manager.RemoveNodes("PresetSaturations");

			if (hues == null || saturations == null)
				yield break;

			var colors = new Color[hues.Length];
			for (var i = 0; i < colors.Length; i++)
				colors[i] = Color.FromAhsv(hues[i], saturations[i], v);

			manager.AddNode("PresetColors", colors);
		}
	}
}
