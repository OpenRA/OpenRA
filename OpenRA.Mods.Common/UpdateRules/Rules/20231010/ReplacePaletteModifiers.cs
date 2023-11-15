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
	public class ReplacePaletteModifiers : UpdateRule
	{
		public override string Name => "Replace palette modifiers with post-processing shaders.";

		public override string Description =>
			"MenuPaletteEffect is renamed to MenuPostProcessEffect\n" +
			"ChronoshiftPaletteEffect is renamed to ChronoshiftPostProcessEffect\n" +
			"FlashPaletteEffect is renamed to FlashPostProcessEffect\n" +
			"GlobalLightingPaletteEffect is renamed to TintPostProcessEffect\n" +
			"D2kFogPalette is removed\n" +
			"PaletteFromScaledPalette is removed";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			actorNode.RenameChildrenMatching("MenuPaletteEffect", "MenuPostProcessEffect");
			actorNode.RenameChildrenMatching("ChronoshiftPaletteEffect", "ChronoshiftPostProcessEffect");
			actorNode.RenameChildrenMatching("FlashPaletteEffect", "FlashPostProcessEffect");
			actorNode.RenameChildrenMatching("GlobalLightingPaletteEffect", "TintPostProcessEffect");
			actorNode.RemoveNodes("D2kFogPalette");
			actorNode.RemoveNodes("PaletteFromScaledPalette");

			yield break;
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNodeBuilder weaponNode)
		{
			foreach (var warheadNode in weaponNode.ChildrenMatching("Warhead"))
				if (warheadNode.Value.Value == "FlashPaletteEffect")
					warheadNode.Value.Value = "FlashEffect";

			yield break;
		}
	}
}
