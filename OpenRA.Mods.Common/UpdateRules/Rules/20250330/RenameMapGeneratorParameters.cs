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
	/// <summary>
	/// Renames generator Settings nodes to Options or Parameters.
	/// </summary>
	public class RenameMapGeneratorParameters : UpdateRule
	{
		public override string Name => "Add labels to MarkerLayerOverlay colors.";
		public override string Description => "Renames generator Settings nodes to Options or Parameters.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			var generatorTraits = (string[])["ClassicMapGenerator", "ClearMapGenerator", "D2kMapGenerator", "TSMapGenerator"];
			foreach (var generator in generatorTraits)
			{
				foreach (var generatorNode in actorNode.ChildrenMatching(generator))
				{
					foreach (var optionsNode in generatorNode.ChildrenMatching("Settings"))
					{
						optionsNode.RenameKey("Options");
						foreach (var mcoNode in optionsNode.ChildrenMatching("MultiChoiceOption"))
							foreach (var choiceNode in mcoNode.ChildrenMatching("Choice"))
								choiceNode.RenameChildrenMatching("Settings", "Parameters");
					}
				}
			}

			yield break;
		}
	}
}
