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
	public class ReplaceShadowPalette : UpdateRule
	{
		public override string Name => "Removed ShadowPalette from WithShadow and projectiles.";

		public override string Description =>
			"The ShadowPalette field has been replaced by ShadowColor on projectiles.\n" +
			"The Palette field on WithShadow and ShadowPalette on WithParachute have similarly been replaced with ShadowColor.";

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "The shadow palette overrides have been removed from the following locations:\n" +
					UpdateUtils.FormatMessageList(locations) + "\n\n" +
					"You may wish to inspect and change these.";

			locations.Clear();
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			foreach (var projectileNode in weaponNode.ChildrenMatching("Projectile"))
				if (projectileNode.RemoveNodes("ShadowPalette") > 0)
					locations.Add($"{weaponNode.Key}: {weaponNode.Key} ({weaponNode.Location.Filename})");

			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var node in actorNode.ChildrenMatching("WithShadow"))
				if (node.RemoveNodes("Palette") > 0)
					locations.Add($"{actorNode.Key}: {node.Key} ({actorNode.Location.Filename})");

			foreach (var node in actorNode.ChildrenMatching("WithParachute"))
				if (node.RemoveNodes("ShadowPalette") > 0)
					locations.Add($"{actorNode.Key}: {node.Key} ({actorNode.Location.Filename})");

			yield break;
		}
	}
}
